#include <Windows.h>
#include <stdio.h>
#include <time.h>
#include <conio.h>
#include "pch.h"
void ShowError(DWORD ErrorCode){
    printf("返回值:");
    switch(ErrorCode){
    case ERROR_BEGINNING_OF_MEDIA:
        printf("尝试在中等开始标记失败之前访问数据");
        break;
    case ERROR_BUS_RESET:
        printf("在总线上检测到重置条件");
        break;
    case ERROR_DEVICE_NOT_PARTITIONED:
        printf("加载磁带时找不到分区信息");
        break;
    case ERROR_DEVICE_REQUIRES_CLEANING:
        printf("磁带驱动器能够报告它需要清洁，并报告它确实需要清洁");
        break;
    case ERROR_END_OF_MEDIA:
        printf("操作期间到达磁带结束标记");
        break;
    case ERROR_FILEMARK_DETECTED:
        printf("操作期间已到达文件标记");
        break;
    case ERROR_INVALID_BLOCK_LENGTH:
        printf("多卷分区中新磁带上的块大小不正确");
        break;
    case ERROR_MEDIA_CHANGED:
        printf("驱动器中的磁带已被替换或删除");
        break;
    case ERROR_NO_DATA_DETECTED:
        printf("操作期间已到达数据结束标记");
        break;
    case ERROR_NO_MEDIA_IN_DRIVE:
        printf("驱动器中没有媒体");
        break;
    case ERROR_NOT_SUPPORTED:
        printf("磁带驱动程序不支持请求的函数");
        break;
    case ERROR_PARTITION_FAILURE:
        printf("无法对磁带进行分区");
        break;
    case ERROR_SETMARK_DETECTED:
        printf("操作期间已达到一个设置标记");
        break;
    case ERROR_UNABLE_TO_LOCK_MEDIA:
        printf("尝试锁定弹出机制失败");
        break;
    case ERROR_UNABLE_TO_UNLOAD_MEDIA:
        printf("尝试卸载磁带失败");
        break;
    case ERROR_WRITE_PROTECT:
        printf("媒体受写入保护");
        break;
    case 0:
        printf("正常");
        break;
    default:
        printf("未知错误:%d",ErrorCode);
        break;
    }
    printf("\n");
    return;
}
void YesNo(int b){
    if(b==TRUE)
        printf("是\n");
    else
        printf("否\n");
    return;
}
int cmdmain(){
    BOOL isReadonly=TRUE;
    char TapePath[260],File1[260];
    TAPE_GET_MEDIA_PARAMETERS Temp_TG;
    TAPE_SET_MEDIA_PARAMETERS Temp_TS;
    TAPE_GET_DRIVE_PARAMETERS Temp_DG;
    TAPE_SET_DRIVE_PARAMETERS Temp_DS;
    time_t start,end;
    unsigned int i,Temp_INT;
    char RWPower,command[32],temp[64],buf[65536];
    LARGE_INTEGER Blocks,FileSize,Templ;
    unsigned __int64 Temp_64;
    HANDLE hTape,hFile1;
    DWORD BlockLow,BlockHigh,Temp_DWORD;
    printf("磁带只因路径(例:\\\\.\\TAPE0):");
    gets(TapePath);
    printf("以只读方式打开?(Y/N):");
    RWPower=getch();
    if(RWPower=='Y' || RWPower=='y')
        hTape=CreateFileA(TapePath,GENERIC_READ,FILE_SHARE_READ,NULL,OPEN_EXISTING,0,NULL);
    else{
        hTape=CreateFileA(TapePath,GENERIC_READ | GENERIC_WRITE,FILE_SHARE_READ,NULL,OPEN_EXISTING,0,NULL);
        isReadonly=FALSE;
    }
    if(hTape==INVALID_HANDLE_VALUE){
        printf("\n打不开磁带只因! 错误码:%d\n",GetLastError());
        system("pause");
        return -1;
    }
    printf("\n磁带只因打开成功!\n");
    while(1){
        printf(">");
        gets(command);
        if(!strcmp(command,"status")){
            ShowError(GetTapeStatus(hTape));
        }else if(!strcmp(command,"getposition")){
            ShowError(GetTapePosition(hTape,TAPE_ABSOLUTE_POSITION,&Temp_DWORD,&BlockLow,&BlockHigh));
            Templ.LowPart=BlockLow;
            Templ.HighPart=BlockHigh;
            printf("磁头位置:0x%08X%08X %llu 低位:0x%08X %u 高位:0x%08X %u\n",BlockHigh,BlockLow,Templ.QuadPart,BlockLow,BlockLow,BlockHigh,BlockHigh);
        }else if(!strcmp(command,"setposition")){
            printf("低块地址:");
            scanf("%u",&BlockLow);
            printf("高块地址:");
            scanf("%u",&BlockHigh);
            ShowError(SetTapePosition(hTape,TAPE_ABSOLUTE_BLOCK,0,BlockLow,BlockHigh,FALSE));
        }else if(!strcmp(command,"gettapeinfo")){
            memset(&Temp_TG,0,sizeof(Temp_TG));
            ShowError(GetTapeParameters(hTape,GET_TAPE_MEDIA_INFORMATION,&Temp_DWORD,&Temp_TG));
            printf("磁带字节总数:%llu\n",Temp_TG.Capacity.QuadPart);
            printf("剩余磁带字节数:%llu\n",Temp_TG.Remaining.QuadPart);
            printf("每块的字节数:%u\n",Temp_TG.BlockSize);
            printf("磁带的分区数:%u\n",Temp_TG.PartitionCount);
            if(Temp_TG.WriteProtected==TRUE)
                printf("磁带是否写保护:是\n");
            else
                printf("磁带是否写保护:否\n");
        }else if(!strcmp(command,"getdriveinfo")){
            memset(&Temp_DG,0,sizeof(Temp_TG));
            ShowError(GetTapeParameters(hTape,GET_TAPE_DRIVE_INFORMATION,&Temp_DWORD,&Temp_DG));
            printf("是否支持硬件错误更正:");
            YesNo(Temp_DG.ECC);
            printf("是否启用硬件数据压缩:");
            YesNo(Temp_DG.Compression);
            printf("是否启用数据填充:");
            YesNo(Temp_DG.DataPadding);
            printf("是否启用setmark报告:");
            YesNo(Temp_DG.ReportSetmarks);
            printf("设备的默认块大小:%u\n",Temp_DG.DefaultBlockSize);
            printf("设备的最大块大小:%u\n",Temp_DG.MaximumBlockSize);
            printf("设备的最小块大小:%u\n",Temp_DG.MinimumBlockSize);
            printf("可在设备上创建的最大分区数:%u\n",Temp_DG.MaximumPartitionCount);
            printf("设备功能标志的低序位(懒得解码):0x%08X\n设备功能标志的高序位(懒得解码):0x%08X\n",Temp_DG.FeaturesLow,Temp_DG.FeaturesHigh);
            printf("磁带结束警告与磁带的物理端之间的字节数:%u\n",Temp_DG.EOTWarningZoneSize);
        }else if(!strcmp(command,"settapeblocksize")){
            printf("块大小:");
            scanf("%u",&Temp_DWORD);
            memset(&Temp_TS,0,sizeof(Temp_TS));
            Temp_TS.BlockSize=Temp_DWORD;
            ShowError(SetTapeParameters(hTape,SET_TAPE_MEDIA_INFORMATION,&Temp_TS));
        }else if(!strcmp(command,"writetape")){
            if(isReadonly)
                printf("当前模式不支持此功能\n");
            else{
                printf("转储到磁带的文件路径(是文件,例:C:\\Test.img):");
                gets(File1);
                hFile1=CreateFileA(File1,GENERIC_READ,FILE_SHARE_READ,NULL,OPEN_EXISTING,0,NULL);
                if(hFile1==INVALID_HANDLE_VALUE){
                    printf("打不开文件 错误码:%d\n",GetLastError());
                }else{
                    memset(&Temp_DG,0,sizeof(Temp_TG));
                    ShowError(GetTapeParameters(hTape,GET_TAPE_MEDIA_INFORMATION,&Temp_DWORD,&Temp_TG));
                    if(Temp_TG.BlockSize!=65536){
                        printf("块大小不是65536或遇到其它错误! 使用settapeblocksize调整块大小\n");
                    }else{
                        GetFileSizeEx(hFile1,&FileSize);
                        //start=time(NULL);
                        for(Temp_64=1;Temp_64<=(FileSize.QuadPart/65536)+1;Temp_64++){
                            memset(buf,0,sizeof(buf));
                            ReadFile(hFile1,&buf,sizeof(buf),&Temp_DWORD,NULL);
                            Temp_INT=GetLastError();
                            if(Temp_INT==0){
                                WriteFile(hTape,&buf,sizeof(buf),&Temp_DWORD,NULL);
                                Temp_INT=GetLastError();
                                if(Temp_INT!=0){
                                    printf("在写入磁带时遇到错误 错误码:%d\n",Temp_INT);
                                    break;
                                }
                            }else{
                                printf("在读取文件时遇到错误 错误码:%d\n",Temp_INT);
                                break;
                            }
                        }
                        //end=time(NULL);
                        //printf("用时%d秒 速度:%d MB/s\n",(end-start)/1000,((FileSize.QuadPart/1024)/((end-start)+1))/1024000);
                    }
                    CloseHandle(hFile1);
                }
            }
        }else if(!strcmp(command,"readtape")){
            printf("转储磁带文件保存在哪(是文件,例:C:\\Test.img,如已有文件,则覆盖)?");
            gets(File1);
            printf("读几个块(低):");
            scanf("%u",&BlockLow);
            printf("读几个块(高):");
            scanf("%u",&BlockHigh);
            Templ.LowPart=BlockLow;
            Templ.HighPart=BlockHigh;
            hFile1=CreateFileA(File1,GENERIC_READ | GENERIC_WRITE,0,NULL,CREATE_ALWAYS,0,NULL);
            if(hFile1==INVALID_HANDLE_VALUE){
                printf("打不开文件 错误码:%d\n",GetLastError());
            }else{
                memset(&Temp_DG,0,sizeof(Temp_TG));
                ShowError(GetTapeParameters(hTape,GET_TAPE_MEDIA_INFORMATION,&Temp_DWORD,&Temp_TG));
                if(Temp_TG.BlockSize!=65536){
                    printf("块大小不是65536或遇到其它错误! 使用settapeblocksize调整块大小\n");
                }else{
                    //start=time(NULL);
                    for(Temp_64=1;Temp_64<=Templ.QuadPart;Temp_64++){
                        memset(buf,0,sizeof(buf));
                        ReadFile(hTape,&buf,sizeof(buf),&Temp_DWORD,NULL);
                        Temp_INT=GetLastError();
                        if(Temp_INT==0){
                            WriteFile(hFile1,&buf,sizeof(buf),&Temp_DWORD,NULL);
                            Temp_INT=GetLastError();
                            if(Temp_INT!=0){
                                printf("在读取磁带时遇到错误 错误码:%d\n",Temp_INT);
                                break;
                            }
                        }else{
                            printf("在写入文件时遇到错误 错误码:%d\n",Temp_INT);
                            break;
                        }
                    }
                    //end=time(NULL);
                    //printf("用时%d秒 速度:%d MB/s\n",FileSize.QuadPart/((end-start)/1000));
                }
                CloseHandle(hFile1);
            }
        }else if(!strcmp(command,"load")){
            ShowError(PrepareTape(hTape,TAPE_LOAD,FALSE));
        }else if(!strcmp(command,"unload")){
            ShowError(PrepareTape(hTape,TAPE_UNLOAD,FALSE));
        }else if(!strcmp(command,"lock")){
            ShowError(PrepareTape(hTape,TAPE_LOCK,FALSE));
        }else if(!strcmp(command,"unlock")){
            ShowError(PrepareTape(hTape,TAPE_UNLOCK,FALSE));
        }else if(!strcmp(command,"tension")){
            ShowError(PrepareTape(hTape,TAPE_TENSION,FALSE));
        }else if(!strcmp(command,"writelongfilemark")){
            printf("磁带标记数:");
            scanf("%u",&Temp_DWORD);
            ShowError(WriteTapemark(hTape,TAPE_LONG_FILEMARKS,Temp_DWORD,FALSE));
        }else if(!strcmp(command,"writeshortfilemark")){
            printf("磁带标记数:");
            scanf("%u",&Temp_DWORD);
            ShowError(WriteTapemark(hTape,TAPE_SHORT_FILEMARKS,Temp_DWORD,FALSE));
        }else if(!strcmp(command,"writesetmark")){
            printf("磁带标记数:");
            scanf("%u",&Temp_DWORD);
            ShowError(WriteTapemark(hTape,TAPE_SETMARKS,Temp_DWORD,FALSE));
        }else if(!strcmp(command,"writefilemark")){
            printf("磁带标记数:");
            scanf("%u",&Temp_DWORD);
            ShowError(WriteTapemark(hTape,TAPE_FILEMARKS,Temp_DWORD,FALSE));
        }else if(!strcmp(command,"rewind")){
            ShowError(SetTapePosition(hTape,TAPE_REWIND,0,0,0,FALSE));
        }else if(!strcmp(command,"gotofilemarks")){
            printf("文件标记数(低位):");
            scanf("%u",&BlockLow);
            printf("文件标记数(高位):");
            scanf("%u",&BlockHigh);
            ShowError(SetTapePosition(hTape,TAPE_SPACE_FILEMARKS,0,BlockLow,BlockHigh,FALSE));
        }else if(!strcmp(command,"gotosetmarks")){
            printf("设置标记数(低位):");
            scanf("%u",&BlockLow);
            printf("设置标记数(高位):");
            scanf("%u",&BlockHigh);
            ShowError(SetTapePosition(hTape,TAPE_SPACE_SETMARKS,0,BlockLow,BlockHigh,FALSE));
        }else if(!strcmp(command,"createpartition")){
            printf("分区的数量(getdriveinfo命令提供磁带可以支持的最大分区数,分别除最后一个分区外,最后一个分区的大小是磁带的剩余部分):");
            scanf("%u",&BlockLow);
            printf("分区的大小(MB):");
            scanf("%u",&BlockHigh);
            ShowError(CreateTapePartition(hTape,TAPE_INITIATOR_PARTITIONS,BlockLow,BlockHigh));
        }else if(!strcmp(command,"erasetape")){
            printf("真的要在这里写入数据结束标示吗,后面的数据将会丢失(Y/N)?");
            if(getch()=='Y' || getch()=='y'){
                printf("\n请稍候...");
                ShowError(EraseTape(hTape,TAPE_ERASE_SHORT,FALSE));
            }else{
                printf("\n被用户取消");
            }
        }
        else{
            printf("未知的命令:%s\n",command);
        }
    }
    return 0;
}