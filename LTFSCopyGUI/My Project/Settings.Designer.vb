﻿Option Strict On
Option Explicit On
'------------------------------------------------------------------------------
' <auto-generated>
'     此代码由工具生成。
'     运行时版本:4.0.30319.42000
'
'     对此文件的更改可能会导致不正确的行为，并且如果
'     重新生成代码，这些更改将会丢失。
' </auto-generated>
'------------------------------------------------------------------------------

Imports System.ComponentModel

Namespace My

    <Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),
     Global.System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0"),
     Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>
    Partial Friend NotInheritable Class MySettings
        Inherits Global.System.Configuration.ApplicationSettingsBase

        Public Shared defaultInstance As MySettings = CType(Global.System.Configuration.ApplicationSettingsBase.Synchronized(New MySettings()), MySettings)

#Region "My.Settings 自动保存功能"
#If _MyType = "WindowsForms" Then
        Private Shared addedHandler As Boolean

        Private Shared addedHandlerLockObject As New Object

        <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>
        Private Shared Sub AutoSaveSettings(sender As Global.System.Object, e As Global.System.EventArgs)
            If My.Application.SaveMySettingsOnExit Then
                My.Settings.Save()
            End If
        End Sub
#End If
#End Region

        Public Shared ReadOnly Property [Default]() As MySettings
            Get

#If _MyType = "WindowsForms" Then
                If Not addedHandler Then
                    SyncLock addedHandlerLockObject
                        If Not addedHandler Then
                            AddHandler My.Application.Shutdown, AddressOf AutoSaveSettings
                            addedHandler = True
                        End If
                    End SyncLock
                End If
#End If
                Return defaultInstance
            End Get
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("C:\tmp\ltfs\LT0007.schema")>
        <Category("IndexAnalyzer")>
        <LocalizedDescription("PropertyDescription_IndexAnalyzer_LastFile")>
        Public Property IndexAnalyzer_LastFile() As String
            Get
                Return CType(Me("IndexAnalyzer_LastFile"), String)
            End Get
            Set
                Me("IndexAnalyzer_LastFile") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("L:\")>
        <Category("IndexAnalyzer")>
        <LocalizedDescription("PropertyDescription_IndexAnalyzer_Src")>
        Public Property IndexAnalyzer_Src() As String
            Get
                Return CType(Me("IndexAnalyzer_Src"), String)
            End Get
            Set
                Me("IndexAnalyzer_Src") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("U:\")>
        <Category("IndexAnalyzer")>
        <LocalizedDescription("PropertyDescription_IndexAnalyzer_Dest")>
        Public Property IndexAnalyzer_Dest() As String
            Get
                Return CType(Me("IndexAnalyzer_Dest"), String)
            End Get
            Set
                Me("IndexAnalyzer_Dest") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <LocalizedDescription("PropertyDescription_HashTaskWindow_ReHash")>
        <Category("HashTaskWindow")>
        Public Property HashTaskWindow_ReHash() As Boolean
            Get
                Return CType(Me("HashTaskWindow_ReHash"), Boolean)
            End Get
            Set
                Me("HashTaskWindow_ReHash") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("IndexAnalyzer")>
        <LocalizedDescription("PropertyDescription_IndexAnalyzer_GenCMD")>
        Public Property IndexAnalyzer_GenCMD() As Boolean
            Get
                Return CType(Me("IndexAnalyzer_GenCMD"), Boolean)
            End Get
            Set
                Me("IndexAnalyzer_GenCMD") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("True")>
        <Category("FileBrowser")>
        <LocalizedDescription("PropertyDescription_FileBrowser_CopyInfo")>
        Public Property FileBrowser_CopyInfo() As Boolean
            Get
                Return CType(Me("FileBrowser_CopyInfo"), Boolean)
            End Get
            Set
                Me("FileBrowser_CopyInfo") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("覆盖已有文件"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_OverwriteExist")>
        Public Property LTFSWriter_OverwriteExist() As Boolean
            Get
                Return CType(Me("LTFSWriter_OverwriteExist"), Boolean)
            End Get
            Set
                Me("LTFSWriter_OverwriteExist") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("写入完成后：" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "  0-什么都不做" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "  1-更新数据区索引" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "  2-更新全部索引" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "  3-更新全部索引并弹出" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("1")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_OnWriteFinished")>
        Public Property LTFSWriter_OnWriteFinished() As Byte
            Get
                Return CType(Me("LTFSWriter_OnWriteFinished"), Byte)
            End Get
            Set
                Me("LTFSWriter_OnWriteFinished") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("容量损失自动停顿"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_AutoFlush")>
        Public Property LTFSWriter_AutoFlush() As Boolean
            Get
                Return CType(Me("LTFSWriter_AutoFlush"), Boolean)
            End Get
            Set
                Me("LTFSWriter_AutoFlush") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("文件标签设置"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_FileLabel")>
        Public Property LTFSWriter_FileLabel() As String
            Get
                Return CType(Me("LTFSWriter_FileLabel"), String)
            End Get
            Set
                Me("LTFSWriter_FileLabel") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("启用日志"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_LogEnabled")>
        Public Property LTFSWriter_LogEnabled() As Boolean
            Get
                Return CType(Me("LTFSWriter_LogEnabled"), Boolean)
            End Get
            Set
                Me("LTFSWriter_LogEnabled") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("总是更新数据区索引"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_ForceIndex")>
        Public Property LTFSWriter_ForceIndex() As Boolean
            Get
                Return CType(Me("LTFSWriter_ForceIndex"), Boolean)
            End Get
            Set
                Me("LTFSWriter_ForceIndex") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("True")>
        <Category("LTFSConfigurator")>
        <LocalizedDescription("PropertyDescription_LTFSConf_AutoRefresh")>
        Public Property LTFSConf_AutoRefresh() As Boolean
            Get
                Return CType(Me("LTFSConf_AutoRefresh"), Boolean)
            End Get
            Set
                Me("LTFSConf_AutoRefresh") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("重装带前清洁次数"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("3")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_CleanCycle")>
        Public Property LTFSWriter_CleanCycle() As Integer
            Get
                Return CType(Me("LTFSWriter_CleanCycle"), Integer)
            End Get
            Set
                Me("LTFSWriter_CleanCycle") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("计算校验"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("True")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_HashOnWriting")>
        Public Property LTFSWriter_HashOnWriting() As Boolean
            Get
                Return CType(Me("LTFSWriter_HashOnWriting"), Boolean)
            End Get
            Set
                Me("LTFSWriter_HashOnWriting") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("异步校验"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("True")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_HashAsync")>
        Public Property LTFSWriter_HashAsync() As Boolean
            Get
                Return CType(Me("LTFSWriter_HashAsync"), Boolean)
            End Get
            Set
                Me("LTFSWriter_HashAsync") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("索引更新间隔（字节）"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("38654705664")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_IndexWriteInterval")>
        Public Property LTFSWriter_IndexWriteInterval() As Long
            Get
                Return CType(Me("LTFSWriter_IndexWriteInterval"), Long)
            End Get
            Set
                Me("LTFSWriter_IndexWriteInterval") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("容量显示刷新间隔"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("1")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_CapacityRefreshInterval")>
        Public Property LTFSWriter_CapacityRefreshInterval() As Integer
            Get
                Return CType(Me("LTFSWriter_CapacityRefreshInterval"), Integer)
            End Get
            Set
                Me("LTFSWriter_CapacityRefreshInterval") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("预读文件数"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("0")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_PreLoadFileCount")>
        Public Property LTFSWriter_PreLoadFileCount() As Integer
            Get
                Return CType(Me("LTFSWriter_PreLoadFileCount"), Integer)
            End Get
            Set
                Me("LTFSWriter_PreLoadFileCount") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("预读字节数"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("8388608")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_PreLoadBytes")>
        Public Property LTFSWriter_PreLoadBytes() As Integer
            Get
                Return CType(Me("LTFSWriter_PreLoadBytes"), Integer)
            End Get
            Set
                Me("LTFSWriter_PreLoadBytes") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("禁用分区"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_DisablePartition")>
        Public Property LTFSWriter_DisablePartition() As Boolean
            Get
                Return CType(Me("LTFSWriter_DisablePartition"), Boolean)
            End Get
            Set
                Me("LTFSWriter_DisablePartition") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute(" 非商业许可")>
        <Category("Application")>
        <LocalizedDescription("PropertyDescription_Application_License")>
        Public Property Application_License() As String
            Get
                Return CType(Me("Application_License"), String)
            End Get
            Set
                Me("Application_License") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("容量损失判定速度下限"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("60")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_AutoCleanDownLim")>
        Public Property LTFSWriter_AutoCleanDownLim() As Double
            Get
                Return CType(Me("LTFSWriter_AutoCleanDownLim"), Double)
            End Get
            Set
                Me("LTFSWriter_AutoCleanDownLim") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("容量损失判定速度上限"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("87")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_AutoCleanUpperLim")>
        Public Property LTFSWriter_AutoCleanUpperLim() As Double
            Get
                Return CType(Me("LTFSWriter_AutoCleanUpperLim"), Double)
            End Get
            Set
                Me("LTFSWriter_AutoCleanUpperLim") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("容量损失判定秒数"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("3")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_AutoCleanTimeThreashould")>
        Public Property LTFSWriter_AutoCleanTimeThreashould() As Integer
            Get
                Return CType(Me("LTFSWriter_AutoCleanTimeThreashould"), Integer)
            End Get
            Set
                Me("LTFSWriter_AutoCleanTimeThreashould") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("去重"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_DeDupe")>
        Public Property LTFSWriter_DeDupe() As Boolean
            Get
                Return CType(Me("LTFSWriter_DeDupe"), Boolean)
            End Get
            Set
                Me("LTFSWriter_DeDupe") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("显示容量损失"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_ShowLoss")>
        Public Property LTFSWriter_ShowLoss() As Boolean
            Get
                Return CType(Me("LTFSWriter_ShowLoss"), Boolean)
            End Get
            Set
                Me("LTFSWriter_ShowLoss") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("跳过符号链接"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("True")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_SkipSymlink")>
        Public Property LTFSWriter_SkipSymlink() As Boolean
            Get
                Return CType(Me("LTFSWriter_SkipSymlink"), Boolean)
            End Get
            Set
                Me("LTFSWriter_SkipSymlink") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("显示文件数"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("False")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_ShowFileCount")>
        Public Property LTFSWriter_ShowFileCount() As Boolean
            Get
                Return CType(Me("LTFSWriter_ShowFileCount"), Boolean)
            End Get
            Set
                Me("LTFSWriter_ShowFileCount") = Value
            End Set
        End Property

        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("写入开始时切换电源策略"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_PowerPolicyOnWriteBegin")>
        Public Property LTFSWriter_PowerPolicyOnWriteBegin() As Global.System.Guid
            Get
                Return CType(Me("LTFSWriter_PowerPolicyOnWriteBegin"), Global.System.Guid)
            End Get
            Set
                Me("LTFSWriter_PowerPolicyOnWriteBegin") = Value
            End Set
        End Property

        '''<summary>
        '''写入结束后切换电源策略
        '''</summary>
        <Global.System.Configuration.UserScopedSettingAttribute(),
         Global.System.Configuration.SettingsDescriptionAttribute("写入结束后切换电源策略"),
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
         Global.System.Configuration.DefaultSettingValueAttribute("381b4222-f694-41f0-9685-ff5bb260df2e")>
        <Category("LTFSWriter")>
        <LocalizedDescription("PropertyDescription_LTFSWriter_PowerPolicyOnWriteEnd")>
        Public Property LTFSWriter_PowerPolicyOnWriteEnd() As Global.System.Guid
            Get
                Return CType(Me("LTFSWriter_PowerPolicyOnWriteEnd"), Global.System.Guid)
            End Get
            Set
                Me("LTFSWriter_PowerPolicyOnWriteEnd") = Value
            End Set
        End Property
    End Class
End Namespace

Namespace My

    <Global.Microsoft.VisualBasic.HideModuleNameAttribute(),
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>
    Friend Module MySettingsProperty

        <Global.System.ComponentModel.Design.HelpKeywordAttribute("My.Settings")>
        Friend ReadOnly Property Settings() As Global.LTFSCopyGUI.My.MySettings
            Get
                Return Global.LTFSCopyGUI.My.MySettings.Default
            End Get
        End Property
    End Module
End Namespace
