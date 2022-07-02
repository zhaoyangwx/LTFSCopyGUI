# LTFSCopyGUI
LTFS文件排序复制工具

适用HP LTFS

读文件前需先Load-Eject一次磁带以刷新schema文件，默认位置在C:\tmp\ltfs

读取schema文件，对文件存放的block进行排序，并产生命令行用来复制文件。

若Partition A有文件，先复制Partition A的文件

新增校验功能，可覆盖保存原schema文件

新增复制功能，边校验边复制文件
