<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FileBrowser
    Inherits System.Windows.Forms.Form

    'Form 重写 Dispose，以清理组件列表。
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows 窗体设计器所必需的
    Private components As System.ComponentModel.IContainer

    '注意: 以下过程是 Windows 窗体设计器所必需的
    '可以使用 Windows 窗体设计器修改它。  
    '不要使用代码编辑器修改它。
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FileBrowser))
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.全选ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.按大小ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.匹配文件名ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.TreeView1 = New LTFSCopyGUI.TreeViewEx()
        Me.ContextMenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ImageList1
        '
        Me.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit
        resources.ApplyResources(Me.ImageList1, "ImageList1")
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        '
        'Button1
        '
        resources.ApplyResources(Me.Button1, "Button1")
        Me.Button1.Name = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        resources.ApplyResources(Me.Button2, "Button2")
        Me.Button2.Name = "Button2"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.全选ToolStripMenuItem, Me.按大小ToolStripMenuItem, Me.匹配文件名ToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        resources.ApplyResources(Me.ContextMenuStrip1, "ContextMenuStrip1")
        '
        '全选ToolStripMenuItem
        '
        Me.全选ToolStripMenuItem.Name = "全选ToolStripMenuItem"
        resources.ApplyResources(Me.全选ToolStripMenuItem, "全选ToolStripMenuItem")
        '
        '按大小ToolStripMenuItem
        '
        Me.按大小ToolStripMenuItem.Name = "按大小ToolStripMenuItem"
        resources.ApplyResources(Me.按大小ToolStripMenuItem, "按大小ToolStripMenuItem")
        '
        '匹配文件名ToolStripMenuItem
        '
        Me.匹配文件名ToolStripMenuItem.Name = "匹配文件名ToolStripMenuItem"
        resources.ApplyResources(Me.匹配文件名ToolStripMenuItem, "匹配文件名ToolStripMenuItem")
        '
        'CheckBox1
        '
        resources.ApplyResources(Me.CheckBox1, "CheckBox1")
        Me.CheckBox1.Checked = True
        Me.CheckBox1.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'TreeView1
        '
        resources.ApplyResources(Me.TreeView1, "TreeView1")
        Me.TreeView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TreeView1.CheckBoxes = True
        Me.TreeView1.Name = "TreeView1"
        '
        'FileBrowser
        '
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.TreeView1)
        Me.DoubleBuffered = True
        Me.Name = "FileBrowser"
        Me.ContextMenuStrip1.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents TreeView1 As TreeViewEx
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents 全选ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 按大小ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 匹配文件名ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CheckBox1 As CheckBox

    Public Sub New()
        DisplayHelper.BeforeInitializeComponent(Me)
        ' 此调用是设计器所必需的。
        InitializeComponent()
        ' 在 InitializeComponent() 调用之后添加任何初始化。
        DisplayHelper.AfterInitializeComponent(Me)
    End Sub
End Class
