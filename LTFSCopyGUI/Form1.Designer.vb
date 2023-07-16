<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.查找ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.错误检查ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.未校验检查ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.合并文件ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TextBox3 = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.TextBox4 = New System.Windows.Forms.TextBox()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button5 = New System.Windows.Forms.Button()
        Me.Button6 = New System.Windows.Forms.Button()
        Me.Button7 = New System.Windows.Forms.Button()
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.Button8 = New System.Windows.Forms.Button()
        Me.Button9 = New System.Windows.Forms.Button()
        Me.Button10 = New System.Windows.Forms.Button()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.SchemaLoadText = New System.Windows.Forms.ListBox()
        Me.FormTitle = New System.Windows.Forms.Label()
        Me.ContextMenuStrip1.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TextBox1
        '
        resources.ApplyResources(Me.TextBox1, "TextBox1")
        Me.TextBox1.Name = "TextBox1"
        '
        'Label1
        '
        resources.ApplyResources(Me.Label1, "Label1")
        Me.Label1.Name = "Label1"
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
        Me.Button2.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Button2.Name = "Button2"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'ContextMenuStrip1
        '
        resources.ApplyResources(Me.ContextMenuStrip1, "ContextMenuStrip1")
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.查找ToolStripMenuItem, Me.错误检查ToolStripMenuItem, Me.未校验检查ToolStripMenuItem, Me.合并文件ToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        '
        '查找ToolStripMenuItem
        '
        resources.ApplyResources(Me.查找ToolStripMenuItem, "查找ToolStripMenuItem")
        Me.查找ToolStripMenuItem.Name = "查找ToolStripMenuItem"
        '
        '错误检查ToolStripMenuItem
        '
        resources.ApplyResources(Me.错误检查ToolStripMenuItem, "错误检查ToolStripMenuItem")
        Me.错误检查ToolStripMenuItem.Name = "错误检查ToolStripMenuItem"
        '
        '未校验检查ToolStripMenuItem
        '
        resources.ApplyResources(Me.未校验检查ToolStripMenuItem, "未校验检查ToolStripMenuItem")
        Me.未校验检查ToolStripMenuItem.Name = "未校验检查ToolStripMenuItem"
        '
        '合并文件ToolStripMenuItem
        '
        resources.ApplyResources(Me.合并文件ToolStripMenuItem, "合并文件ToolStripMenuItem")
        Me.合并文件ToolStripMenuItem.Name = "合并文件ToolStripMenuItem"
        '
        'TextBox2
        '
        resources.ApplyResources(Me.TextBox2, "TextBox2")
        Me.TextBox2.Name = "TextBox2"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        resources.ApplyResources(Me.OpenFileDialog1, "OpenFileDialog1")
        '
        'Label2
        '
        resources.ApplyResources(Me.Label2, "Label2")
        Me.Label2.Name = "Label2"
        '
        'TextBox3
        '
        resources.ApplyResources(Me.TextBox3, "TextBox3")
        Me.TextBox3.Name = "TextBox3"
        '
        'Label3
        '
        resources.ApplyResources(Me.Label3, "Label3")
        Me.Label3.Name = "Label3"
        '
        'TextBox4
        '
        resources.ApplyResources(Me.TextBox4, "TextBox4")
        Me.TextBox4.Name = "TextBox4"
        '
        'CheckBox1
        '
        resources.ApplyResources(Me.CheckBox1, "CheckBox1")
        Me.CheckBox1.Checked = True
        Me.CheckBox1.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Button3
        '
        resources.ApplyResources(Me.Button3, "Button3")
        Me.Button3.Name = "Button3"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'SaveFileDialog1
        '
        resources.ApplyResources(Me.SaveFileDialog1, "SaveFileDialog1")
        '
        'Label4
        '
        resources.ApplyResources(Me.Label4, "Label4")
        Me.Label4.Name = "Label4"
        '
        'Button4
        '
        resources.ApplyResources(Me.Button4, "Button4")
        Me.Button4.Name = "Button4"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button5
        '
        resources.ApplyResources(Me.Button5, "Button5")
        Me.Button5.Name = "Button5"
        Me.Button5.UseVisualStyleBackColor = True
        '
        'Button6
        '
        resources.ApplyResources(Me.Button6, "Button6")
        Me.Button6.Name = "Button6"
        Me.Button6.UseVisualStyleBackColor = True
        '
        'Button7
        '
        resources.ApplyResources(Me.Button7, "Button7")
        Me.Button7.Name = "Button7"
        Me.Button7.UseVisualStyleBackColor = True
        '
        'FolderBrowserDialog1
        '
        resources.ApplyResources(Me.FolderBrowserDialog1, "FolderBrowserDialog1")
        '
        'Button8
        '
        resources.ApplyResources(Me.Button8, "Button8")
        Me.Button8.Name = "Button8"
        Me.Button8.UseVisualStyleBackColor = True
        '
        'Button9
        '
        resources.ApplyResources(Me.Button9, "Button9")
        Me.Button9.Name = "Button9"
        Me.Button9.UseVisualStyleBackColor = True
        '
        'Button10
        '
        resources.ApplyResources(Me.Button10, "Button10")
        Me.Button10.Name = "Button10"
        Me.Button10.UseVisualStyleBackColor = True
        '
        'CheckBox2
        '
        resources.ApplyResources(Me.CheckBox2, "CheckBox2")
        Me.CheckBox2.Checked = True
        Me.CheckBox2.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        resources.ApplyResources(Me.GroupBox1, "GroupBox1")
        Me.GroupBox1.Controls.Add(Me.SchemaLoadText)
        Me.GroupBox1.Controls.Add(Me.FormTitle)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.TabStop = False
        '
        'SchemaLoadText
        '
        resources.ApplyResources(Me.SchemaLoadText, "SchemaLoadText")
        Me.SchemaLoadText.FormattingEnabled = True
        Me.SchemaLoadText.Items.AddRange(New Object() {resources.GetString("SchemaLoadText.Items"), resources.GetString("SchemaLoadText.Items1"), resources.GetString("SchemaLoadText.Items2"), resources.GetString("SchemaLoadText.Items3"), resources.GetString("SchemaLoadText.Items4"), resources.GetString("SchemaLoadText.Items5"), resources.GetString("SchemaLoadText.Items6")})
        Me.SchemaLoadText.Name = "SchemaLoadText"
        '
        'FormTitle
        '
        resources.ApplyResources(Me.FormTitle, "FormTitle")
        Me.FormTitle.Name = "FormTitle"
        '
        'Form1
        '
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.CheckBox2)
        Me.Controls.Add(Me.Button10)
        Me.Controls.Add(Me.Button9)
        Me.Controls.Add(Me.Button8)
        Me.Controls.Add(Me.Button7)
        Me.Controls.Add(Me.Button6)
        Me.Controls.Add(Me.Button5)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.TextBox4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.TextBox3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.TextBox2)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.TextBox1)
        Me.Name = "Form1"
        Me.ContextMenuStrip1.ResumeLayout(False)
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents Label2 As Label
    Friend WithEvents TextBox3 As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents TextBox4 As TextBox
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Button3 As Button
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents Label4 As Label
    Friend WithEvents Button4 As Button
    Friend WithEvents Button5 As Button
    Friend WithEvents Button6 As Button
    Friend WithEvents Button7 As Button
    Friend WithEvents FolderBrowserDialog1 As FolderBrowserDialog
    Friend WithEvents Button8 As Button
    Friend WithEvents Button9 As Button
    Friend WithEvents Button10 As Button
    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents 查找ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 错误检查ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 合并文件ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents 未校验检查ToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CheckBox2 As CheckBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents FormTitle As Label
    Friend WithEvents SchemaLoadText As ListBox
End Class
