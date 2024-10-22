# LTFSCopyGUI

![LTFSCopyGUIIcon](https://user-images.githubusercontent.com/32697586/177280874-14110415-bd43-4e54-94fa-e8a16673d755.png)

## LTFS文件排序复制工具

### 主要功能

#### LTFSCopyGUI.exe：索引排序生成复制脚本

排序生成脚本功能适用HP LTFS

根据离线索引schema文件，对文件存放的block进行排序，并产生命令行用来复制文件。若Partition A有文件，先复制Partition A的文件。

#### LTFSConfigurator.exe：磁带挂载管理/直接读写

盘符挂载适用HPLTFS

直接读写适用HP/IBM或者第三方OEM驱动器  

**${\color{red}{\textrm{使用直接读写功能时，请勿挂载盘符}}}$**

如果OEM驱动器没有安装驱动，可以使用设备路径例如\\\\.\GLOBALROOT\Device\00000043

#### LTFSCopyGUI.exe CLI

LTFSCopyGUI.exe /?查看命令行用法

##### 目前支持功能
    LTFSCopyGUI.exe -t
    LTFSCopyGUI.exe -rb
    LTFSCopyGUI.exe -wb
    LTFSCopyGUI.exe -raw
    LTFSCopyGUI.exe -mkltfs

### 更新说明

1. 最初的功能：排序生成脚本

2. 新增校验功能，可覆盖保存原schema文件(建议另存到别处，防止被覆盖)

3. 新增复制功能，边校验边复制文件

4. 更多驱动器控制功能、SCSI命令直接发送、磁带标签修改、磁带信息读取

LtfsCommand from **[inaxeon/ltfscmd](https://github.com/inaxeon/ltfscmd)**

5. 新增LTFS直接读写（无需挂载），LTO4模拟LTFS数据区功能，FTP服务器（只读）

### 关于加密

如需启用加密可在加载磁带后使用 **[VulpesSARL/LTOEnc](https://github.com/VulpesSARL/LTOEnc)** 或者直接发送SECURITY PROTOCOL相关SCSI指令设置驱动器密钥

对于开启加密的磁带，请不要启用自动重装带（重装带前清洁次数改成0禁用），否则重新装带会重置驱动器加密密钥导致写入失败。

### How to switch language:
    lang.ini to set language (Currently en for English, zh for Chinese Simplified. zh Default)
    if no lang.ini exist, will follow system language setting

---

演示视频（bilibili）：**[BV1j24y177PF](https://www.bilibili.com/video/BV1j24y177PF)**  **[BV1Gy4y1f7WP](https://www.bilibili.com/video/BV1Gy4y1f7WP)**

---


**欢迎加入LTO磁带技术交流QQ群 433387693 获取开发中的最新版本，以及相关资料**

软著登字第11348107号

## 如何赞助
    【闲鱼】https://m.tb.cn/h.gwpMFmp?tk=lxLr3mCtsUx MF6563 「我在闲鱼发布了【LTFSCopyGUI软件商用许可/技术支持/个性化冠名服务】」
    点击链接直接打开
