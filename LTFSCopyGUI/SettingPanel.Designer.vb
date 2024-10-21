<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SettingPanel
    Inherits System.Windows.Forms.Form

    'Form 重写 Dispose，以清理组件列表。
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SettingPanel))
        Me.PropertyGrid1 = New System.Windows.Forms.PropertyGrid()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.文件ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.打开ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.保存ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'PropertyGrid1
        '
        resources.ApplyResources(Me.PropertyGrid1, "PropertyGrid1")
        Me.PropertyGrid1.Name = "PropertyGrid1"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.文件ToolStripMenuItem})
        resources.ApplyResources(Me.MenuStrip1, "MenuStrip1")
        Me.MenuStrip1.Name = "MenuStrip1"
        '
        '文件ToolStripMenuItem
        '
        Me.文件ToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.打开ToolStripMenuItem, Me.保存ToolStripMenuItem})
        Me.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem"
        resources.ApplyResources(Me.文件ToolStripMenuItem, "文件ToolStripMenuItem")
        '
        '打开ToolStripMenuItem
        '
        Me.打开ToolStripMenuItem.Name = "打开ToolStripMenuItem"
        resources.ApplyResources(Me.打开ToolStripMenuItem, "打开ToolStripMenuItem")
        '
        '保存ToolStripMenuItem
        '
        Me.保存ToolStripMenuItem.Name = "保存ToolStripMenuItem"
        resources.ApplyResources(Me.保存ToolStripMenuItem, "保存ToolStripMenuItem")
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "Application.config"
        resources.ApplyResources(Me.OpenFileDialog1, "OpenFileDialog1")
        '
        'SaveFileDialog1
        '
        Me.SaveFileDialog1.FileName = "Application.config"
        resources.ApplyResources(Me.SaveFileDialog1, "SaveFileDialog1")
        '
        'SettingPanel
        '
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.PropertyGrid1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "SettingPanel"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PropertyGrid1 As PropertyGrid
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents 文件ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 打开ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 保存ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
End Class
