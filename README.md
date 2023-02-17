# LTFSCopyGUI

![LTFSCopyGUIIcon](https://user-images.githubusercontent.com/32697586/177280874-14110415-bd43-4e54-94fa-e8a16673d755.png)

## LTFS文件排序复制工具

适用HP LTFS

读文件前需先Load-Eject一次磁带以刷新schema文件，默认位置在C:\tmp\ltfs

读取schema文件，对文件存放的block进行排序，并产生命令行用来复制文件。

若Partition A有文件，先复制Partition A的文件

新增校验功能，可覆盖保存原schema文件(建议另存到别处，防止被覆盖)

新增复制功能，边校验边复制文件

LtfsCommand from **[inaxeon/ltfscmd](https://github.com/inaxeon/ltfscmd)**

更多驱动器控制功能、SCSI命令直接发送、磁带标签修改、磁带信息读取

新增LTFS直接读写（无需挂载），LTO4模拟LTFS数据区功能


---


**欢迎加入LTO磁带技术交流QQ群 433387693**
