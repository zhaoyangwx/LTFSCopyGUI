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
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.全选ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.按大小ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.匹配文件名ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TreeView1 = New LTFSCopyGUI.TreeViewEx()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.ContextMenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ImageList1
        '
        Me.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit
        Me.ImageList1.ImageSize = New System.Drawing.Size(16, 16)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        '
        'Button1
        '
        Me.Button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button1.Location = New System.Drawing.Point(323, 419)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "OK"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button2.Location = New System.Drawing.Point(404, 419)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 2
        Me.Button2.Text = "Cancel"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.全选ToolStripMenuItem, Me.按大小ToolStripMenuItem, Me.匹配文件名ToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(137, 70)
        '
        '全选ToolStripMenuItem
        '
        Me.全选ToolStripMenuItem.Name = "全选ToolStripMenuItem"
        Me.全选ToolStripMenuItem.Size = New System.Drawing.Size(136, 22)
        Me.全选ToolStripMenuItem.Text = "全选"
        '
        '按大小ToolStripMenuItem
        '
        Me.按大小ToolStripMenuItem.Name = "按大小ToolStripMenuItem"
        Me.按大小ToolStripMenuItem.Size = New System.Drawing.Size(136, 22)
        Me.按大小ToolStripMenuItem.Text = "按大小"
        '
        '匹配文件名ToolStripMenuItem
        '
        Me.匹配文件名ToolStripMenuItem.Name = "匹配文件名ToolStripMenuItem"
        Me.匹配文件名ToolStripMenuItem.Size = New System.Drawing.Size(136, 22)
        Me.匹配文件名ToolStripMenuItem.Text = "匹配文件名"
        '
        'TreeView1
        '
        Me.TreeView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TreeView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TreeView1.CheckBoxes = True
        Me.TreeView1.Location = New System.Drawing.Point(12, 12)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(776, 401)
        Me.TreeView1.TabIndex = 0
        '
        'CheckBox1
        '
        Me.CheckBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Checked = True
        Me.CheckBox1.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox1.Location = New System.Drawing.Point(12, 426)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(72, 16)
        Me.CheckBox1.TabIndex = 3
        Me.CheckBox1.Text = "CopyInfo"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'FileBrowser
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.TreeView1)
        Me.Name = "FileBrowser"
        Me.Text = "FileBrowser"
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
End Class
