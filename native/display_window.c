/* Blueberry window/viewport control. GPL-3.0-only.
 * Source pixels come from negotiated sink caps, never from a requested preset. */
#include "display_window.h"
#include <gst/video/videooverlay.h>
#include <windows.h>
#include <stdio.h>

static HWND window;
static HANDLE ready;
static INIT_ONCE initialized = INIT_ONCE_STATIC_INIT;
static CRITICAL_SECTION lock;
static GstElement *sink;
static int video_w, video_h, mode = 2, state, pan_x, pan_y;
static RECT restored = {100,100,1000,800};
static wchar_t command_path[MAX_PATH];
static unsigned long long last_revision;
static int rectangle_ok;
static RECT rendered;
static int arranging, redraws;

/* ResizeBuffers and child-window moves are asynchronous in D3D11. Redraw
 * after they finish, including when the sender has stopped sending frames. */
static void request_redraw(void) {
    redraws=4;
    SetTimer(window,2,60,NULL);
}

static void adjust_frame(RECT *rect,LONG style,HWND hwnd) {
    AdjustWindowRectExForDpi(rect,style,FALSE,0,GetDpiForWindow(hwnd));
}

static void viewport(void) {
    if (!window || IsIconic(window)) return;
    RECT r; GetClientRect(window, &r);
    /* A deferred redraw can race with minimization/restoration. GStreamer
     * rejects zero-size rectangles; wait for the next nonempty WM_SIZE. */
    if(r.right<=0 || r.bottom<=0)return;
    EnterCriticalSection(&lock);
    GstElement *target = sink ? GST_ELEMENT(gst_object_ref(sink)) : NULL;
    LeaveCriticalSection(&lock);
    if (target && video_w > 0 && video_h > 0) {
        if (mode == 0) {
            int max_x = video_w > r.right ? video_w-r.right : 0;
            int max_y = video_h > r.bottom ? video_h-r.bottom : 0;
            if (pan_x > max_x) pan_x = max_x;
            if (pan_y > max_y) pan_y = max_y;
            if (pan_x < 0) pan_x = 0;
            if (pan_y < 0) pan_y = 0;
            rendered.left=max_x ? -pan_x : (r.right-video_w)/2;
            rendered.top=max_y ? -pan_y : (r.bottom-video_h)/2;
            rendered.right=rendered.left+video_w;rendered.bottom=rendered.top+video_h;
        } else {
            rendered=r;
        }
        rectangle_ok=gst_video_overlay_set_render_rectangle(GST_VIDEO_OVERLAY(target),rendered.left,rendered.top,
            rendered.right-rendered.left,rendered.bottom-rendered.top);
        gst_video_overlay_expose(GST_VIDEO_OVERLAY(target));
    }
    if (target) gst_object_unref(target);
}

static void resize_window(void) {
    if (!window || video_w <= 0 || video_h <= 0) return;
    MONITORINFO monitor = {sizeof(MONITORINFO)};
    GetMonitorInfoW(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), &monitor);
    LONG style = state == 2 ? WS_POPUP : WS_OVERLAPPEDWINDOW;
    /* Replacing the frame style must not clear WS_VISIBLE. Console commands
     * call ShowWindow afterwards, but Escape/system restore do not. Clearing
     * visibility leaves the D3D11 child occluded until another command shows it.
     * Preserve hidden windows too: resizing alone must never reopen them. */
    style |= GetWindowLongPtrW(window,GWL_STYLE) & WS_VISIBLE;
    arranging=1;
    /* SetWindowPos alone does not clear WS_MAXIMIZE or restore placement. */
    if(IsZoomed(window) || IsIconic(window))ShowWindow(window,SW_RESTORE);
    SetWindowLongPtrW(window, GWL_STYLE, style);
    if(state==1){ShowWindow(window,SW_MAXIMIZE);arranging=0;viewport();request_redraw();return;}
    RECT area = state == 2 ? monitor.rcMonitor : monitor.rcWork;
    if (state == 0) {
        if (mode == 0) {
            RECT r = {0,0,video_w,video_h}; adjust_frame(&r,style,window);
            SetWindowPos(window, HWND_NOTOPMOST, restored.left, restored.top,
                r.right-r.left,r.bottom-r.top,SWP_FRAMECHANGED|SWP_NOACTIVATE);
        } else {
            int w=restored.right-restored.left, h=restored.bottom-restored.top;
            RECT frame={0,0,0,0}; adjust_frame(&frame,style,window);
            int fw=frame.right-frame.left,fh=frame.bottom-frame.top;
            if (mode == 1) {
                double scale=(w-fw)/(double)video_w;
                double max_w=(area.right-area.left-fw)/(double)video_w;
                double max_h=(area.bottom-area.top-fh)/(double)video_h;
                if(scale>max_w)scale=max_w;if(scale>max_h)scale=max_h;
                w=(int)(video_w*scale)+fw;h=(int)(video_h*scale)+fh;
            } else {
                if(w>area.right-area.left)w=area.right-area.left;
                if(h>area.bottom-area.top)h=area.bottom-area.top;
            }
            int x=restored.left,y=restored.top;
            if(x+w>area.right)x=area.right-w;if(y+h>area.bottom)y=area.bottom-h;
            if(x<area.left)x=area.left;if(y<area.top)y=area.top;
            SetWindowPos(window, HWND_NOTOPMOST,x,y,w,h,SWP_FRAMECHANGED|SWP_NOACTIVATE);
        }
    } else {
        int w=area.right-area.left,h=area.bottom-area.top;
        SetWindowPos(window,state==2?HWND_TOPMOST:HWND_NOTOPMOST,
            area.left+(area.right-area.left-w)/2,area.top+(area.bottom-area.top-h)/2,
            w,h,SWP_FRAMECHANGED|SWP_NOACTIVATE);
    }
    arranging=0;viewport();request_redraw();
}

static void apply_display(int m,int s) {
    if(state==0 && s!=0)GetWindowRect(window,&restored);
    mode=m;state=s;pan_x=pan_y=0;resize_window();
    if(video_w>0){ShowWindow(window,SW_SHOW);SetForegroundWindow(window);}
}

static LRESULT CALLBACK procedure(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp) {
    switch(msg) {
#ifdef BLUEBERRY_TEST
    case WM_APP+2: apply_display((int)wp,(int)lp);return 0;
    case WM_APP+3: {
        int *values=(int*)lp;RECT r;GetClientRect(hwnd,&r);
        values[0]=video_w;values[1]=video_h;values[2]=r.right;values[3]=r.bottom;
        values[4]=rendered.right-rendered.left;values[5]=rendered.bottom-rendered.top;
        values[6]=rectangle_ok;values[7]=state;return 0;
    }
#endif
    case WM_APP+1:
        video_w=(int)wp;video_h=(int)lp;pan_x=pan_y=0;
        resize_window();ShowWindow(hwnd,SW_SHOWNOACTIVATE);return 0;
    case WM_TIMER: {
        if(wp==2){viewport();if(--redraws<=0)KillTimer(hwnd,2);return 0;}
        int m,s;unsigned long long revision;FILE *f=_wfopen(command_path,L"r");
        if(f) {int n=fscanf(f,"%d %d %llu",&m,&s,&revision);fclose(f);
            if(n==3 && m>=0 && m<=2 && s>=0 && s<=2 && revision!=last_revision) {
                last_revision=revision;apply_display(m,s);
            }
        } return 0; }
    case WM_SYSCOMMAND:
        if((wp&0xfff0)==SC_CLOSE) {ShowWindow(hwnd,SW_HIDE);return 0;}
        if((wp&0xfff0)==SC_MAXIMIZE) {if(state==0)GetWindowRect(hwnd,&restored);state=1;resize_window();return 0;}
        if((wp&0xfff0)==SC_RESTORE && state!=0) {state=0;resize_window();return 0;}
        break;
    case WM_KEYDOWN:
        if(wp==VK_ESCAPE && state!=0) {state=0;resize_window();return 0;}
        if(mode==0) {
            if(wp==VK_LEFT)pan_x-=50;if(wp==VK_RIGHT)pan_x+=50;
            if(wp==VK_UP)pan_y-=50;if(wp==VK_DOWN)pan_y+=50;viewport();
        } return 0;
    case WM_LBUTTONDOWN: SetFocus(hwnd);return 0;
    case WM_MOUSEWHEEL:
        if(mode==0) {int delta=(short)HIWORD(wp)>0?-80:80;
            if(GetKeyState(VK_SHIFT)&0x8000)pan_x+=delta;else pan_y+=delta;viewport();return 0;}break;
    case WM_GETMINMAXINFO: {
        MINMAXINFO *info=(MINMAXINFO*)lp;
        info->ptMinTrackSize.x=240;info->ptMinTrackSize.y=160;
        if(video_w>0 && video_h>0) {
            MONITORINFO monitor={sizeof(MONITORINFO)};
            GetMonitorInfoW(MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST),&monitor);
            RECT frame={0,0,0,0};adjust_frame(&frame,WS_OVERLAPPEDWINDOW,hwnd);
            int fw=frame.right-frame.left,fh=frame.bottom-frame.top;
            int aw=monitor.rcWork.right-monitor.rcWork.left,ah=monitor.rcWork.bottom-monitor.rcWork.top;
            int caption=fh-fw;
            if(mode==1){
                double minimum=(240-fw)/(double)video_w;
                if((160-fh)/(double)video_h>minimum)minimum=(160-fh)/(double)video_h;
                info->ptMinTrackSize.x=(int)(video_w*minimum+0.5)+fw;
                info->ptMinTrackSize.y=(int)(video_h*minimum+0.5)+fh;
                /* Default independent desktop limits clamp one edge after
                 * WM_SIZING and break the ratio for portrait streams. */
                info->ptMaxTrackSize.x=100000;info->ptMaxTrackSize.y=100000;
            }
            double scale=aw/(double)video_w;
            if((ah-caption)/(double)video_h<scale)scale=(ah-caption)/(double)video_h;
            int cw=mode==1?(int)(video_w*scale):aw,ch=mode==1?(int)(video_h*scale):ah-caption;
            info->ptMaxSize.x=cw+fw;info->ptMaxSize.y=ch+fh;
            info->ptMaxPosition.x=monitor.rcWork.left-monitor.rcMonitor.left+(aw-cw)/2-fw/2;
            info->ptMaxPosition.y=monitor.rcWork.top-monitor.rcMonitor.top+(ah-ch-caption)/2-fw/2;
        }
        if(mode==0 && state==0 && video_w>0) {info->ptMaxTrackSize.x=video_w+100>240?video_w+100:240;info->ptMaxTrackSize.y=video_h+100>160?video_h+100:160;}
        break; }
    case WM_ENTERSIZEMOVE:
        if(!IsZoomed(hwnd) && state!=2)state=0;
        break;
    case WM_SIZING:
        if(mode==1 && state!=2 && !IsZoomed(hwnd) && video_w>0 && video_h>0) {
            state=0;
            RECT *r=(RECT*)lp,frame={0,0,0,0};adjust_frame(&frame,GetWindowLongPtrW(hwnd,GWL_STYLE),hwnd);
            int fw=frame.right-frame.left,fh=frame.bottom-frame.top;
            if(wp==WMSZ_TOP || wp==WMSZ_BOTTOM)r->right=r->left+(int)((r->bottom-r->top-fh)*(double)video_w/video_h+0.5)+fw;
            else {int h=(int)((r->right-r->left-fw)*(double)video_h/video_w+0.5)+fh;
                if(wp==WMSZ_TOPLEFT || wp==WMSZ_TOPRIGHT)r->top=r->bottom-h;else r->bottom=r->top+h;}
            return TRUE;
        }break;
    case WM_WINDOWPOSCHANGING: {
        WINDOWPOS *pos=(WINDOWPOS*)lp;
        /* Also constrain programmatic/snap resizing, not only WM_SIZING.
         * Native maximize and our fullscreen layout have their own rules. */
        if(!arranging && mode==1 && state!=2 && !IsZoomed(hwnd) &&
           !(pos->flags&SWP_NOSIZE) && video_w>0 && video_h>0) {
            RECT frame={0,0,0,0},current;
            adjust_frame(&frame,GetWindowLongPtrW(hwnd,GWL_STYLE),hwnd);
            GetWindowRect(hwnd,&current);
            int fw=frame.right-frame.left,fh=frame.bottom-frame.top;
            if(pos->cx==current.right-current.left)
                pos->cx=(int)((pos->cy-fh)*(double)video_w/video_h+0.5)+fw;
            else pos->cy=(int)((pos->cx-fw)*(double)video_h/video_w+0.5)+fh;
            state=0;
        }break; }
    case WM_EXITSIZEMOVE:
        if(state==0)GetWindowRect(hwnd,&restored);request_redraw();return 0;
    case WM_DPICHANGED: if(state==0)restored=*(RECT*)lp;resize_window();return 0;
    case WM_SIZE: if(wp!=SIZE_MINIMIZED){viewport();request_redraw();}return 0;
    case WM_SHOWWINDOW: if(wp)request_redraw();break;
    case WM_ACTIVATE:
        if(state==2)SetWindowPos(hwnd,LOWORD(wp)==WA_INACTIVE?HWND_NOTOPMOST:HWND_TOPMOST,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE);
        break;
    case WM_ERASEBKGND: {RECT r;GetClientRect(hwnd,&r);FillRect((HDC)wp,&r,(HBRUSH)GetStockObject(BLACK_BRUSH));return 1;}
    case WM_CLOSE: ShowWindow(hwnd,SW_HIDE);return 0;
    }
    return DefWindowProcW(hwnd,msg,wp,lp);
}

static DWORD WINAPI window_thread(LPVOID unused) {
    typedef HANDLE (WINAPI *DpiFn)(HANDLE);
    DpiFn dpi=(DpiFn)GetProcAddress(GetModuleHandleW(L"user32.dll"),"SetThreadDpiAwarenessContext");
    if(dpi)dpi((HANDLE)-4);
    WNDCLASSW wc={0};wc.lpfnWndProc=procedure;wc.hInstance=GetModuleHandleW(NULL);
    wc.lpszClassName=L"BlueberryVideoWindow";wc.hCursor=LoadCursor(NULL,IDC_ARROW);
    wc.hbrBackground=(HBRUSH)GetStockObject(BLACK_BRUSH);
    GetModuleFileNameW(NULL,command_path,MAX_PATH);
    wchar_t *slash=wcsrchr(command_path,L'\\');
    if(slash){wcscpy(slash+1,L"resources\\icon.ico");wc.hIcon=(HICON)LoadImageW(NULL,command_path,IMAGE_ICON,0,0,LR_LOADFROMFILE|LR_DEFAULTSIZE);
        wcscpy(slash+1,L"display-mode.txt");}
    RegisterClassW(&wc);
    window=CreateWindowW(wc.lpszClassName,L"蓝莓投屏",WS_OVERLAPPEDWINDOW,100,100,900,700,NULL,NULL,wc.hInstance,NULL);
    SetTimer(window,1,250,NULL);SetEvent(ready);
    MSG message;while(GetMessageW(&message,NULL,0,0)>0){TranslateMessage(&message);DispatchMessageW(&message);}
    return 0;
}

static BOOL CALLBACK initialize_window(PINIT_ONCE once, PVOID parameter, PVOID *context) {
    InitializeCriticalSection(&lock);
    ready=CreateEvent(NULL,TRUE,FALSE,NULL);
    if(!ready)return FALSE;
    HANDLE thread=CreateThread(NULL,0,window_thread,NULL,0,NULL);
    if(!thread){CloseHandle(ready);ready=NULL;return FALSE;}
    CloseHandle(thread);return TRUE;
}

static GstPadProbeReturn caps_probe(GstPad *pad,GstPadProbeInfo *info,gpointer data) {
    GstEvent *event=GST_PAD_PROBE_INFO_EVENT(info);
    if(GST_EVENT_TYPE(event)==GST_EVENT_CAPS) {
        GstCaps *caps;gst_event_parse_caps(event,&caps);
        const GstStructure *s=gst_caps_get_structure(caps,0);int w,h;
        if(gst_structure_get_int(s,"width",&w)&&gst_structure_get_int(s,"height",&h))PostMessageW(window,WM_APP+1,w,h);
    }return GST_PAD_PROBE_OK;
}

static GstBusSyncReply prepare(GstBus *bus,GstMessage *message,gpointer data) {
    if(!gst_is_video_overlay_prepare_window_handle_message(message))return GST_BUS_PASS;
    if(!InitOnceExecuteOnce(&initialized,initialize_window,NULL,NULL))return GST_BUS_PASS;
    if(WaitForSingleObject(ready,5000)!=WAIT_OBJECT_0 || !window)return GST_BUS_PASS;
    GstElement *target=GST_ELEMENT(GST_MESSAGE_SRC(message));
    EnterCriticalSection(&lock);
    GstElement *previous=sink;sink=NULL;
    LeaveCriticalSection(&lock);
    /* Finalization may dispatch window messages; never unref while holding lock. */
    if(previous)gst_object_unref(previous);
    EnterCriticalSection(&lock);
    sink=GST_ELEMENT(gst_object_ref(target));
    LeaveCriticalSection(&lock);
    g_object_set(target,"force-aspect-ratio",TRUE,NULL);
    if(g_object_class_find_property(G_OBJECT_GET_CLASS(target),"fullscreen-toggle-mode"))
        g_object_set(target,"fullscreen-toggle-mode",0,NULL);
    gst_video_overlay_set_window_handle(GST_VIDEO_OVERLAY(target),(guintptr)window);
    GstPad *pad=gst_element_get_static_pad(target,"sink");
    if(pad){gst_pad_add_probe(pad,GST_PAD_PROBE_TYPE_EVENT_DOWNSTREAM,caps_probe,NULL,NULL);
        GstCaps *caps=gst_pad_get_current_caps(pad);if(caps){const GstStructure *s=gst_caps_get_structure(caps,0);int w,h;
            if(gst_structure_get_int(s,"width",&w)&&gst_structure_get_int(s,"height",&h))PostMessageW(window,WM_APP+1,w,h);
            gst_caps_unref(caps);}gst_object_unref(pad);}
    gst_message_unref(message);return GST_BUS_DROP;
}
void blueberry_display_attach(GstBus *bus) {gst_bus_set_sync_handler(bus,prepare,NULL,NULL);}
