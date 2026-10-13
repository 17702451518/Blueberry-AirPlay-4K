/* Session teardown/reconnect regression without AirPlay ports or user config. */
#include "display_window.h"
#include <windows.h>
#include <stdio.h>

static BOOL CALLBACK find_own_window(HWND window,LPARAM result) {
    DWORD pid;GetWindowThreadProcessId(window,&pid);
    wchar_t name[64];GetClassNameW(window,name,64);
    if(pid==GetCurrentProcessId() && wcscmp(name,L"BlueberryVideoWindow")==0){*(HWND*)result=window;return FALSE;}
    return TRUE;
}

static HWND own_window(void) {HWND window=NULL;EnumWindows(find_own_window,(LPARAM)&window);return window;}

static int wait_visible(HWND window,int visible) {
    for(int i=0;i<100;i++){
        if(!!IsWindowVisible(window)==visible)return 1;
        Sleep(20);
    }
    return 0;
}

static int white_frame(HWND window) {
    RECT rectangle;GetClientRect(window,&rectangle);
    HDC dc=GetDC(window),copy=CreateCompatibleDC(dc);
    HBITMAP bitmap=CreateCompatibleBitmap(dc,rectangle.right,rectangle.bottom);
    HGDIOBJ old=SelectObject(copy,bitmap);
    BOOL captured=PrintWindow(window,copy,3);
    COLORREF pixel=captured?GetPixel(copy,rectangle.right/2,rectangle.bottom/2):CLR_INVALID;
    SelectObject(copy,old);DeleteObject(bitmap);DeleteDC(copy);ReleaseDC(window,dc);
    return pixel!=CLR_INVALID && GetRValue(pixel)>200 && GetGValue(pixel)>200 && GetBValue(pixel)>200;
}

int main(int argc,char **argv) {
    SetProcessDpiAwarenessContext((HANDLE)-4);
    gst_init(&argc,&argv);
    GError *error=NULL;
    GstElement *pipeline=gst_parse_launch("videotestsrc pattern=white ! video/x-raw,width=720,height=1280,framerate=30/1 ! videoconvert ! d3d11videosink",&error);
    if(error || !pipeline)return 1;
    GstBus *bus=gst_element_get_bus(pipeline);
    blueberry_display_attach(bus);
    int count=0;
    for(int mode=0;mode<3;mode++)for(int state=0;state<3;state++) {
        gst_element_set_state(pipeline,GST_STATE_PLAYING);
        if(gst_element_get_state(pipeline,NULL,NULL,10*GST_SECOND)==GST_STATE_CHANGE_FAILURE)return 2;
        HWND window=own_window();
        if(!window || !wait_visible(window,1))return 3;
        SendMessageW(window,WM_APP+2,mode,state);
        gst_element_set_state(pipeline,GST_STATE_PAUSED);
        gst_element_get_state(pipeline,NULL,NULL,5*GST_SECOND);
        Sleep(100);
        if(!IsWindowVisible(window)){fprintf(stderr,"FAIL: pause treated as disconnect\n");return 4;}
        // An inactive codec sink must not terminate the current picture.
        GstElement *inactive=gst_element_factory_make("fakesink",NULL);
        gst_bus_post(bus,gst_message_new_state_changed(GST_OBJECT(inactive),GST_STATE_PAUSED,GST_STATE_READY,GST_STATE_VOID_PENDING));
        gst_object_unref(inactive);Sleep(40);
        if(!IsWindowVisible(window))return 5;
        gst_element_set_state(pipeline,GST_STATE_NULL);
        gst_element_get_state(pipeline,NULL,NULL,5*GST_SECOND);
        if(!wait_visible(window,0)){fprintf(stderr,"FAIL: stopped session leaves visible black window mode=%d state=%d\n",mode,state);return 6;}
        int values[8]={0};SendMessageW(window,WM_APP+3,0,(LPARAM)values);
        if(values[0] || values[1] || values[6])return 7;
        SendMessageW(window,WM_APP+2,(mode+1)%3,(state+1)%3);
        if(IsWindowVisible(window)){fprintf(stderr,"FAIL: idle display change reopened black window\n");return 8;}
        SendMessageW(window,WM_APP+5,0,0);Sleep(80);
        if(IsWindowVisible(window)){fprintf(stderr,"FAIL: old caps reopened idle window\n");return 12;}
        // Reuse the same pipeline, exercising new window-handle and caps events.
        gst_element_set_state(pipeline,GST_STATE_PLAYING);
        gst_element_get_state(pipeline,NULL,NULL,10*GST_SECOND);
        if(!wait_visible(window,1))return 9;
        Sleep(150);SendMessageW(window,WM_APP+3,0,(LPARAM)values);
        if(values[0]!=720 || values[1]!=1280 || !values[6])return 10;
        SendMessageW(window,WM_APP+5,0,0);SendMessageW(window,WM_APP+6,0,0);Sleep(80);
        SendMessageW(window,WM_APP+3,0,(LPARAM)values);
        if(!IsWindowVisible(window) || values[0]!=720 || values[1]!=1280)return 13;
        if(!white_frame(window)){fprintf(stderr,"FAIL: reconnect window visible but no picture\n");return 14;}
        gst_element_set_state(pipeline,GST_STATE_NULL);
        gst_element_get_state(pipeline,NULL,NULL,5*GST_SECOND);
        if(!wait_visible(window,0))return 11;
        count++;
    }
    gst_object_unref(bus);gst_object_unref(pipeline);
    printf("PASS: %d mode/state teardown + pause + inactive sink + idle selection + stale caps + reconnect cases.\n",count);
    return 0;
}
