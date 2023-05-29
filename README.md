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

如需启用加密可在加载磁带后使用 **[VulpesSARL/LTOEnc](https://github.com/VulpesSARL/LTOEnc)** 或者直接发送SECURITY PROTOCOL相关SCSI指令设置驱动器密钥

对于开启加密的磁带，请不要启用容量缺失检测，否则重新装带会重置驱动器加密密钥导致写入失败。

---

演示视频（bilibili）：**[BV1j24y177PF](https://www.bilibili.com/video/BV1j24y177PF)**  **[BV1Gy4y1f7WP](https://www.bilibili.com/video/BV1Gy4y1f7WP)**

---


**欢迎加入LTO磁带技术交流QQ群 433387693**

(C) 2023R11S0790994
