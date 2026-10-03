/* Synthetic D3D11 smoke test. Does not bind AirPlay ports or change user settings. */
#include "display_window.h"
#include <windows.h>
#include <stdio.h>

int main(int argc,char **argv) {
    SetProcessDpiAwarenessContext((HANDLE)-4);
    gst_init(&argc,&argv);
    const int sizes[][2]={{1280,720},{720,1280},{3840,2160},{2160,3840}};
    int count=0;
    for(int i=0;i<4;i++) {
        char description[256];snprintf(description,sizeof(description),
            "videotestsrc pattern=white ! video/x-raw,width=%d,height=%d,framerate=30/1 ! videoconvert ! d3d11videosink",sizes[i][0],sizes[i][1]);
        GError *error=NULL;GstElement *pipeline=gst_parse_launch(description,&error);
        if(error){fprintf(stderr,"pipeline: %s\n",error->message);return 1;}
        GstBus *bus=gst_element_get_bus(pipeline);blueberry_display_attach(bus);
        gst_element_set_state(pipeline,GST_STATE_PLAYING);
        if(gst_element_get_state(pipeline,NULL,NULL,10*GST_SECOND)==GST_STATE_CHANGE_FAILURE)return 2;
        Sleep(600);
        HWND window=FindWindowW(L"BlueberryVideoWindow",NULL);if(!window)return 3;
        /* A paused stream cannot conceal a failed redraw with its next frame. */
        gst_element_set_state(pipeline,GST_STATE_PAUSED);
        gst_element_get_state(pipeline,NULL,NULL,5*GST_SECOND);
        for(int mode=0;mode<3;mode++)for(int state=0;state<3;state++) {
            SendMessageW(window,WM_APP+2,mode,state);Sleep(400);
            int values[8]={0};SendMessageW(window,WM_APP+3,0,(LPARAM)values);
            printf("case %d: mode=%d state=%d source=%dx%d client=%dx%d render=%dx%d accepted=%d\n",i,mode,state,values[0],values[1],values[2],values[3],values[4],values[5],values[6]);fflush(stdout);
            if(values[0]!=sizes[i][0] || values[1]!=sizes[i][1] || !values[6] || values[7]!=state)return 4;
            if(mode==0 && (values[4]!=sizes[i][0] || values[5]!=sizes[i][1]))return 5;
            if(mode==0){HWND child=GetWindow(window,GW_CHILD);RECT pixels;
                if(!child || !GetClientRect(child,&pixels))return 12;
                printf("child %p %ldx%ld\n",child,pixels.right,pixels.bottom);fflush(stdout);
                if(pixels.right!=sizes[i][0] || pixels.bottom!=sizes[i][1])return 12;}
            MONITORINFO monitor={sizeof(MONITORINFO)};GetMonitorInfoW(MonitorFromWindow(window,MONITOR_DEFAULTTONEAREST),&monitor);
            RECT bounds;GetWindowRect(window,&bounds);
            if(state==2 && (bounds.left!=monitor.rcMonitor.left || bounds.top!=monitor.rcMonitor.top ||
                bounds.right!=monitor.rcMonitor.right || bounds.bottom!=monitor.rcMonitor.bottom))return 6;
            if(state==1){POINT origin={0,0};ClientToScreen(window,&origin);
                if(origin.x<monitor.rcWork.left || origin.y<monitor.rcWork.top ||
                    origin.x+values[2]>monitor.rcWork.right || origin.y+values[3]>monitor.rcWork.bottom)return 7;}
            if(state==1 && !IsZoomed(window))return 13;
            if(mode==1 && state!=2 && abs(values[2]*sizes[i][1]-values[3]*sizes[i][0])>(sizes[i][0]>sizes[i][1]?sizes[i][0]:sizes[i][1]))return 8;
            count++;
            HWND child=GetWindow(window,GW_CHILD);RECT client;GetClientRect(window,&client);
            POINT sample={client.right/2,client.bottom/2};MapWindowPoints(window,child,&sample,1);
            HDC dc=GetDC(child),copy=CreateCompatibleDC(dc);
            RECT cr;GetClientRect(child,&cr);
            HBITMAP bitmap=CreateCompatibleBitmap(dc,cr.right,cr.bottom);
            HGDIOBJ old=SelectObject(copy,bitmap);
            BOOL captured=PrintWindow(child,copy,3);
            COLORREF pixel=captured?GetPixel(copy,sample.x,sample.y):CLR_INVALID;
            SelectObject(copy,old);DeleteObject(bitmap);DeleteDC(copy);ReleaseDC(child,dc);
            printf("paused frame pixel: %06lx\n",(unsigned long)pixel);fflush(stdout);
            if(pixel==CLR_INVALID || GetRValue(pixel)<200 || GetGValue(pixel)<200 || GetBValue(pixel)<200)return 19;
        }
        SendMessageW(window,WM_KEYDOWN,VK_F11,0);Sleep(80);
        int toggled[8]={0};SendMessageW(window,WM_APP+3,0,(LPARAM)toggled);if(toggled[7]!=2)return 9;
        SendMessageW(window,WM_KEYDOWN,VK_ESCAPE,0);Sleep(80);
        SendMessageW(window,WM_APP+3,0,(LPARAM)toggled);if(toggled[7]!=0)return 10;
        SendMessageW(window,WM_APP+2,0,2);
        SendMessageW(window,WM_KEYDOWN,VK_RIGHT,0);SendMessageW(window,WM_KEYDOWN,VK_DOWN,0);
        Sleep(80);
        HWND pixelsWindow=GetWindow(window,GW_CHILD);POINT shifted={0,0};MapWindowPoints(pixelsWindow,window,&shifted,1);
        RECT view;GetClientRect(window,&view);
        if(sizes[i][0]>view.right && shifted.x!=-50)return 14;
        if(sizes[i][1]>view.bottom && shifted.y!=-50)return 15;
        SendMessageW(window,WM_APP+3,0,(LPARAM)toggled);if(!toggled[6])return 11;
        SendMessageW(window,WM_APP+2,1,1);SendMessageW(window,WM_SYSCOMMAND,SC_RESTORE,0);
        SendMessageW(window,WM_APP+3,0,(LPARAM)toggled);if(toggled[7]!=0 || IsZoomed(window))return 16;
        SendMessageW(window,WM_SYSCOMMAND,SC_CLOSE,0);if(IsWindowVisible(window))return 17;
        SendMessageW(window,WM_APP+2,2,0);
        if(!IsWindowVisible(window))return 18;
        SendMessageW(window,WM_APP+2,1,0);
        for(int edge=WMSZ_LEFT;edge<=WMSZ_BOTTOMRIGHT;edge++) {
            RECT drag;GetWindowRect(window,&drag);drag.right+=37;drag.bottom+=29;
            SendMessageW(window,WM_SIZING,edge,(LPARAM)&drag);
            SetWindowPos(window,NULL,drag.left,drag.top,drag.right-drag.left,drag.bottom-drag.top,SWP_NOZORDER|SWP_NOACTIVATE);
            SendMessageW(window,WM_APP+3,0,(LPARAM)toggled);
            printf("drag edge=%d client=%dx%d state=%d\n",edge,toggled[2],toggled[3],toggled[7]);fflush(stdout);
            if(abs(toggled[2]*sizes[i][1]-toggled[3]*sizes[i][0])>(sizes[i][0]>sizes[i][1]?sizes[i][0]:sizes[i][1]))return 20;
        }
        gst_element_set_state(pipeline,GST_STATE_NULL);gst_object_unref(bus);gst_object_unref(pipeline);
    }
    printf("PASS: %d D3D11 display combinations, including 4K portrait/landscape.\n",count);return 0;
}
