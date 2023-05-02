<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LTFSConfigurator
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(LTFSConfigurator))
        Me.Button1 = New System.Windows.Forms.Button()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Button6 = New System.Windows.Forms.Button()
        Me.Button7 = New System.Windows.Forms.Button()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Button8 = New System.Windows.Forms.Button()
        Me.Button9 = New System.Windows.Forms.Button()
        Me.Button10 = New System.Windows.Forms.Button()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.CheckBox3 = New System.Windows.Forms.CheckBox()
        Me.Button30 = New System.Windows.Forms.Button()
        Me.Button27 = New System.Windows.Forms.Button()
        Me.Button14 = New System.Windows.Forms.Button()
        Me.Button13 = New System.Windows.Forms.Button()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.ButtonDebugAllowMediaRemoval = New System.Windows.Forms.Button()
        Me.ButtonDebugReleaseUnit = New System.Windows.Forms.Button()
        Me.ButtonDebugFormat = New System.Windows.Forms.Button()
        Me.ButtonDebugDumpIndex = New System.Windows.Forms.Button()
        Me.ComboBox3 = New System.Windows.Forms.ComboBox()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.Button24 = New System.Windows.Forms.Button()
        Me.ButtonDebugDumpTape = New System.Windows.Forms.Button()
        Me.ButtonDebugReadPosition = New System.Windows.Forms.Button()
        Me.ButtonDebugLocate = New System.Windows.Forms.Button()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown()
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.ComboBox2 = New System.Windows.Forms.ComboBox()
        Me.ButtonDebugDumpBuffer = New System.Windows.Forms.Button()
        Me.NumericUpDown7 = New System.Windows.Forms.NumericUpDown()
        Me.ButtonDebugReadBlock = New System.Windows.Forms.Button()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.ButtonDebugRewind = New System.Windows.Forms.Button()
        Me.ButtonDebugDumpMAM = New System.Windows.Forms.Button()
        Me.ButtonDebugReadInfo = New System.Windows.Forms.Button()
        Me.ButtonDebugReadMAM = New System.Windows.Forms.Button()
        Me.NumericUpDown9 = New System.Windows.Forms.NumericUpDown()
        Me.NumericUpDown8 = New System.Windows.Forms.NumericUpDown()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.TextBox10 = New System.Windows.Forms.TextBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.ButtonDebugWriteBarcode = New System.Windows.Forms.Button()
        Me.TextBox9 = New System.Windows.Forms.TextBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.NumericUpDown6 = New System.Windows.Forms.NumericUpDown()
        Me.TextBox8 = New System.Windows.Forms.TextBox()
        Me.TextBox7 = New System.Windows.Forms.TextBox()
        Me.ButtonDebugErase = New System.Windows.Forms.Button()
        Me.ButtonDebugSendSCSICommand = New System.Windows.Forms.Button()
        Me.TextBox6 = New System.Windows.Forms.TextBox()
        Me.TextBox5 = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.SaveFileDialog2 = New System.Windows.Forms.SaveFileDialog()
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.Panel1.SuspendLayout()
        Me.Panel2.SuspendLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown7, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown8, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Button1.Location = New System.Drawing.Point(12, 627)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "刷新"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'ListBox1
        '
        Me.ListBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.HorizontalScrollbar = True
        Me.ListBox1.IntegralHeight = False
        Me.ListBox1.ItemHeight = 12
        Me.ListBox1.Location = New System.Drawing.Point(12, 35)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(289, 586)
        Me.ListBox1.TabIndex = 1
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(12, 6)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 2
        Me.Button2.Text = "启动服务"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(93, 6)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(75, 23)
        Me.Button3.TabIndex = 3
        Me.Button3.Text = "停止服务"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(174, 6)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(75, 23)
        Me.Button4.TabIndex = 4
        Me.Button4.Text = "重挂盘符"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'ComboBox1
        '
        Me.ComboBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.Enabled = False
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(366, 64)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(537, 20)
        Me.ComboBox1.TabIndex = 6
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(307, 38)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(53, 12)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "当前设备"
        '
        'TextBox1
        '
        Me.TextBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox1.Location = New System.Drawing.Point(366, 35)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.ReadOnly = True
        Me.TextBox1.Size = New System.Drawing.Size(537, 21)
        Me.TextBox1.TabIndex = 8
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(307, 67)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(29, 12)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "盘符"
        '
        'Button6
        '
        Me.Button6.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button6.Enabled = False
        Me.Button6.Location = New System.Drawing.Point(909, 62)
        Me.Button6.Name = "Button6"
        Me.Button6.Size = New System.Drawing.Size(75, 23)
        Me.Button6.TabIndex = 10
        Me.Button6.Text = "分配"
        Me.Button6.UseVisualStyleBackColor = True
        '
        'Button7
        '
        Me.Button7.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button7.Enabled = False
        Me.Button7.Location = New System.Drawing.Point(990, 62)
        Me.Button7.Name = "Button7"
        Me.Button7.Size = New System.Drawing.Size(75, 23)
        Me.Button7.TabIndex = 11
        Me.Button7.Text = "删除"
        Me.Button7.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(307, 94)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(29, 12)
        Me.Label3.TabIndex = 12
        Me.Label3.Text = "信息"
        '
        'Button8
        '
        Me.Button8.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button8.Location = New System.Drawing.Point(531, 627)
        Me.Button8.Name = "Button8"
        Me.Button8.Size = New System.Drawing.Size(75, 23)
        Me.Button8.TabIndex = 13
        Me.Button8.Text = "加载磁带"
        Me.Button8.UseVisualStyleBackColor = True
        '
        'Button9
        '
        Me.Button9.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button9.Location = New System.Drawing.Point(772, 627)
        Me.Button9.Name = "Button9"
        Me.Button9.Size = New System.Drawing.Size(75, 23)
        Me.Button9.TabIndex = 14
        Me.Button9.Text = "出仓"
        Me.Button9.UseVisualStyleBackColor = True
        '
        'Button10
        '
        Me.Button10.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button10.Enabled = False
        Me.Button10.Location = New System.Drawing.Point(990, 627)
        Me.Button10.Name = "Button10"
        Me.Button10.Size = New System.Drawing.Size(75, 23)
        Me.Button10.TabIndex = 15
        Me.Button10.Text = "挂载"
        Me.Button10.UseVisualStyleBackColor = True
        '
        'TextBox2
        '
        Me.TextBox2.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox2.Location = New System.Drawing.Point(366, 91)
        Me.TextBox2.Multiline = True
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextBox2.Size = New System.Drawing.Size(699, 530)
        Me.TextBox2.TabIndex = 16
        Me.TextBox2.WordWrap = False
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.CheckBox3)
        Me.Panel1.Controls.Add(Me.Button30)
        Me.Panel1.Controls.Add(Me.Button27)
        Me.Panel1.Controls.Add(Me.Button14)
        Me.Panel1.Controls.Add(Me.Button13)
        Me.Panel1.Controls.Add(Me.CheckBox1)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Controls.Add(Me.Button2)
        Me.Panel1.Controls.Add(Me.TextBox2)
        Me.Panel1.Controls.Add(Me.Button1)
        Me.Panel1.Controls.Add(Me.Button10)
        Me.Panel1.Controls.Add(Me.ListBox1)
        Me.Panel1.Controls.Add(Me.Button9)
        Me.Panel1.Controls.Add(Me.Button3)
        Me.Panel1.Controls.Add(Me.Button8)
        Me.Panel1.Controls.Add(Me.Button4)
        Me.Panel1.Controls.Add(Me.Label3)
        Me.Panel1.Controls.Add(Me.Button7)
        Me.Panel1.Controls.Add(Me.ComboBox1)
        Me.Panel1.Controls.Add(Me.Button6)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.TextBox1)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1077, 662)
        Me.Panel1.TabIndex = 17
        '
        'CheckBox3
        '
        Me.CheckBox3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBox3.AutoSize = True
        Me.CheckBox3.Checked = True
        Me.CheckBox3.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox3.Location = New System.Drawing.Point(93, 631)
        Me.CheckBox3.Name = "CheckBox3"
        Me.CheckBox3.Size = New System.Drawing.Size(72, 16)
        Me.CheckBox3.TabIndex = 23
        Me.CheckBox3.Text = "自动刷新"
        Me.CheckBox3.UseVisualStyleBackColor = True
        '
        'Button30
        '
        Me.Button30.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button30.Location = New System.Drawing.Point(990, 33)
        Me.Button30.Name = "Button30"
        Me.Button30.Size = New System.Drawing.Size(75, 23)
        Me.Button30.TabIndex = 22
        Me.Button30.Text = "排序复制"
        Me.Button30.UseVisualStyleBackColor = True
        '
        'Button27
        '
        Me.Button27.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button27.Location = New System.Drawing.Point(909, 33)
        Me.Button27.Name = "Button27"
        Me.Button27.Size = New System.Drawing.Size(75, 23)
        Me.Button27.TabIndex = 21
        Me.Button27.Text = "直接读写"
        Me.Button27.UseVisualStyleBackColor = True
        '
        'Button14
        '
        Me.Button14.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button14.Location = New System.Drawing.Point(691, 627)
        Me.Button14.Name = "Button14"
        Me.Button14.Size = New System.Drawing.Size(75, 23)
        Me.Button14.TabIndex = 20
        Me.Button14.Text = "仅退带"
        Me.Button14.UseVisualStyleBackColor = True
        '
        'Button13
        '
        Me.Button13.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button13.Location = New System.Drawing.Point(612, 627)
        Me.Button13.Name = "Button13"
        Me.Button13.Size = New System.Drawing.Size(75, 23)
        Me.Button13.TabIndex = 19
        Me.Button13.Text = "仅进仓"
        Me.Button13.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(981, 10)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(84, 16)
        Me.CheckBox1.TabIndex = 0
        Me.CheckBox1.Text = "奇怪的功能"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Panel2
        '
        Me.Panel2.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel2.Controls.Add(Me.ButtonDebugAllowMediaRemoval)
        Me.Panel2.Controls.Add(Me.ButtonDebugReleaseUnit)
        Me.Panel2.Controls.Add(Me.ButtonDebugFormat)
        Me.Panel2.Controls.Add(Me.ButtonDebugDumpIndex)
        Me.Panel2.Controls.Add(Me.ComboBox3)
        Me.Panel2.Controls.Add(Me.CheckBox2)
        Me.Panel2.Controls.Add(Me.Button24)
        Me.Panel2.Controls.Add(Me.ButtonDebugDumpTape)
        Me.Panel2.Controls.Add(Me.ButtonDebugReadPosition)
        Me.Panel2.Controls.Add(Me.ButtonDebugLocate)
        Me.Panel2.Controls.Add(Me.Label16)
        Me.Panel2.Controls.Add(Me.NumericUpDown1)
        Me.Panel2.Controls.Add(Me.NumericUpDown2)
        Me.Panel2.Controls.Add(Me.Label5)
        Me.Panel2.Controls.Add(Me.Label4)
        Me.Panel2.Controls.Add(Me.ComboBox2)
        Me.Panel2.Controls.Add(Me.ButtonDebugDumpBuffer)
        Me.Panel2.Controls.Add(Me.NumericUpDown7)
        Me.Panel2.Controls.Add(Me.ButtonDebugReadBlock)
        Me.Panel2.Controls.Add(Me.Label14)
        Me.Panel2.Controls.Add(Me.ButtonDebugRewind)
        Me.Panel2.Controls.Add(Me.ButtonDebugDumpMAM)
        Me.Panel2.Controls.Add(Me.ButtonDebugReadInfo)
        Me.Panel2.Controls.Add(Me.ButtonDebugReadMAM)
        Me.Panel2.Controls.Add(Me.NumericUpDown9)
        Me.Panel2.Controls.Add(Me.NumericUpDown8)
        Me.Panel2.Controls.Add(Me.Label15)
        Me.Panel2.Controls.Add(Me.Label13)
        Me.Panel2.Controls.Add(Me.TextBox10)
        Me.Panel2.Controls.Add(Me.Label12)
        Me.Panel2.Controls.Add(Me.ButtonDebugWriteBarcode)
        Me.Panel2.Controls.Add(Me.TextBox9)
        Me.Panel2.Controls.Add(Me.Label11)
        Me.Panel2.Controls.Add(Me.Label10)
        Me.Panel2.Controls.Add(Me.Label9)
        Me.Panel2.Controls.Add(Me.Label8)
        Me.Panel2.Controls.Add(Me.NumericUpDown6)
        Me.Panel2.Controls.Add(Me.TextBox8)
        Me.Panel2.Controls.Add(Me.TextBox7)
        Me.Panel2.Controls.Add(Me.ButtonDebugErase)
        Me.Panel2.Controls.Add(Me.ButtonDebugSendSCSICommand)
        Me.Panel2.Controls.Add(Me.TextBox6)
        Me.Panel2.Controls.Add(Me.TextBox5)
        Me.Panel2.Controls.Add(Me.Label7)
        Me.Panel2.Controls.Add(Me.Label6)
        Me.Panel2.Location = New System.Drawing.Point(35, 62)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(991, 547)
        Me.Panel2.TabIndex = 18
        Me.Panel2.Visible = False
        '
        'ButtonDebugAllowMediaRemoval
        '
        Me.ButtonDebugAllowMediaRemoval.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugAllowMediaRemoval.Location = New System.Drawing.Point(5, 404)
        Me.ButtonDebugAllowMediaRemoval.Name = "ButtonDebugAllowMediaRemoval"
        Me.ButtonDebugAllowMediaRemoval.Size = New System.Drawing.Size(119, 23)
        Me.ButtonDebugAllowMediaRemoval.TabIndex = 58
        Me.ButtonDebugAllowMediaRemoval.Text = "AllowMediaRemoval"
        Me.ButtonDebugAllowMediaRemoval.UseVisualStyleBackColor = True
        '
        'ButtonDebugReleaseUnit
        '
        Me.ButtonDebugReleaseUnit.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugReleaseUnit.Location = New System.Drawing.Point(5, 375)
        Me.ButtonDebugReleaseUnit.Name = "ButtonDebugReleaseUnit"
        Me.ButtonDebugReleaseUnit.Size = New System.Drawing.Size(119, 23)
        Me.ButtonDebugReleaseUnit.TabIndex = 57
        Me.ButtonDebugReleaseUnit.Text = "ReleaseUnit"
        Me.ButtonDebugReleaseUnit.UseVisualStyleBackColor = True
        '
        'ButtonDebugFormat
        '
        Me.ButtonDebugFormat.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugFormat.Location = New System.Drawing.Point(270, 521)
        Me.ButtonDebugFormat.Name = "ButtonDebugFormat"
        Me.ButtonDebugFormat.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugFormat.TabIndex = 56
        Me.ButtonDebugFormat.Text = "format"
        Me.ButtonDebugFormat.UseVisualStyleBackColor = True
        '
        'ButtonDebugDumpIndex
        '
        Me.ButtonDebugDumpIndex.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugDumpIndex.Location = New System.Drawing.Point(904, 375)
        Me.ButtonDebugDumpIndex.Name = "ButtonDebugDumpIndex"
        Me.ButtonDebugDumpIndex.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugDumpIndex.TabIndex = 55
        Me.ButtonDebugDumpIndex.Text = "Load index"
        Me.ButtonDebugDumpIndex.UseVisualStyleBackColor = True
        '
        'ComboBox3
        '
        Me.ComboBox3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ComboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox3.FormattingEnabled = True
        Me.ComboBox3.Items.AddRange(New Object() {"Block", "FileMark", "EOD"})
        Me.ComboBox3.Location = New System.Drawing.Point(336, 433)
        Me.ComboBox3.Name = "ComboBox3"
        Me.ComboBox3.Size = New System.Drawing.Size(70, 20)
        Me.ComboBox3.TabIndex = 54
        '
        'CheckBox2
        '
        Me.CheckBox2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBox2.AutoSize = True
        Me.CheckBox2.Location = New System.Drawing.Point(713, 438)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(42, 16)
        Me.CheckBox2.TabIndex = 53
        Me.CheckBox2.Text = "log"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'Button24
        '
        Me.Button24.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Button24.ForeColor = System.Drawing.Color.Red
        Me.Button24.Location = New System.Drawing.Point(683, 433)
        Me.Button24.Name = "Button24"
        Me.Button24.Size = New System.Drawing.Size(24, 23)
        Me.Button24.TabIndex = 52
        Me.Button24.Text = "■"
        Me.Button24.UseVisualStyleBackColor = True
        '
        'ButtonDebugDumpTape
        '
        Me.ButtonDebugDumpTape.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugDumpTape.Location = New System.Drawing.Point(602, 433)
        Me.ButtonDebugDumpTape.Name = "ButtonDebugDumpTape"
        Me.ButtonDebugDumpTape.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugDumpTape.TabIndex = 51
        Me.ButtonDebugDumpTape.Text = "StartDump"
        Me.ButtonDebugDumpTape.UseVisualStyleBackColor = True
        '
        'ButtonDebugReadPosition
        '
        Me.ButtonDebugReadPosition.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugReadPosition.Location = New System.Drawing.Point(904, 404)
        Me.ButtonDebugReadPosition.Name = "ButtonDebugReadPosition"
        Me.ButtonDebugReadPosition.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugReadPosition.TabIndex = 50
        Me.ButtonDebugReadPosition.Text = "Read Pos"
        Me.ButtonDebugReadPosition.UseVisualStyleBackColor = True
        '
        'ButtonDebugLocate
        '
        Me.ButtonDebugLocate.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugLocate.Location = New System.Drawing.Point(521, 433)
        Me.ButtonDebugLocate.Name = "ButtonDebugLocate"
        Me.ButtonDebugLocate.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugLocate.TabIndex = 49
        Me.ButtonDebugLocate.Text = "Locate"
        Me.ButtonDebugLocate.UseVisualStyleBackColor = True
        '
        'Label16
        '
        Me.Label16.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(210, 438)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(59, 12)
        Me.Label16.TabIndex = 47
        Me.Label16.Text = "Partition"
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown1.Location = New System.Drawing.Point(275, 433)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {7, 0, 0, 0})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(55, 21)
        Me.NumericUpDown1.TabIndex = 46
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown2.Location = New System.Drawing.Point(413, 433)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {-1, 0, 0, 0})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(89, 21)
        Me.NumericUpDown2.TabIndex = 45
        '
        'Label5
        '
        Me.Label5.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(761, 438)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(71, 12)
        Me.Label5.TabIndex = 44
        Me.Label5.Text = "Block Limit"
        '
        'Label4
        '
        Me.Label4.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(9, 439)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(29, 12)
        Me.Label4.TabIndex = 43
        Me.Label4.Text = "Read"
        '
        'ComboBox2
        '
        Me.ComboBox2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox2.FormattingEnabled = True
        Me.ComboBox2.Items.AddRange(New Object() {"00h - Main buffer memory", "02h - Burst buffer", "10h - CM EEPROM", "11h - Mechanical EEPROM", "12h - Head assembly EEPROM", "13h - PCA EEPROM", "20h - Main buffer segments 0", "21h - Main buffer segments 1", "22h - Main buffer segments 2", "23h - Main buffer segments 3", "24h - Main buffer segments 4", "25h - Main buffer segments 5", "26h - Main buffer segments 6", "27h - Main buffer segments 7", "28h - Main buffer segments 8", "29h - Main buffer segments 9", "2Ah - Main buffer segments 10", "2Bh - Main buffer segments 11", "2Ch - Main buffer segments 12", "2Dh - Main buffer segments 13", "2Eh - Main buffer segments 14", "2Fh - Main buffer segments 15", "30h - Main buffer segments 16", "31h - Main buffer segments 17", "32h - Main buffer segments 18", "33h - Main buffer segments 19", "34h - Main buffer segments 20", "35h - Main buffer segments 21", "36h - Main buffer segments 22", "37h - Main buffer segments 23", "38h - Main buffer segments 24", "39h - Main buffer segments 25", "3Ah - Main buffer segments 26", "3Bh - Main buffer segments 27", "3Ch - Main buffer segments 28", "3Dh - Main buffer segments 29", "3Eh - Main buffer segments 30", "3Fh - Main buffer segments 31", "40h - Snapshot data buffer", "90h - Mech EPPROM Manufacturing Parameters", "91h - Mech EPPROM Drive Usage Parameters", "93h - Mech EPPROM Drive Usage Parameters(Shipped)", "94h - Mech EPPROM In-House Testing", "95h - Mech EPPROM Servo/Mech Use", "96h - Mech EPPROM Host Access Table", "97h - Mech EPPROM Partner-specific Config Table", "99h - Mech EPPROM Ethernet I/F Table", "9Ah - Mech EPPROM Certificate Table", "9Bh - Mech EPPROM Reserved", "9Ch - Mech EEPROM LTT Drive Health Rules", "9Dh - Mech EEPROM Tape Pull Usage", "A0h - Head EPPROM Manufacturing Parameters", "A1h - Head EPPROM Tuning Parameters", "A2h - Head EPPROM Resistance Parameters", "A3h - Head EPPROM Formatter Data Skew Parameters", "A4h - Head EPPROM In-House Testing", "A5h - Head EPPROM Manufacturing Use", "A6h - Head EPPROM Read/Write Tuning Parameters", "A7h - Head EPPROM Media Usage Table", "A8h - Head EPPROM Reserved", "A9h - Head EPPROM Jabil Production", "AAh - Head EPPROM Reserved", "ABh - Head EEPROM Vendor Information", "B0h - PCA EPPROM Manufacturing Parameters", "B1h - PCA EPPROM Tape Speed Parameters", "B2h - PCA EPPROM Tape Tools Area", "B3h - PCA EPPROM Thermal Data Parameters", "B4h - PCA EPPROM Tape 'A' Log", "B5h - PCA EPPROM Tape 'B' Log", "B6h - PCA EPPROM Tape 'C' Log", "B7h - PCA EPPROM Tape 'D' Log", "B8h - PCA EPPROM Write ERT Logs", "B9h - PCA EPPROM Write Fault Counters", "BAh - PCA EPPROM In-House Testing", "BBh - PCA EPPROM Read ERT Logs", "BCh - PCA EPPROM Persistent Reservation Table", "BDh - PCA EPPROM Host I/F Information Table", "BEh - PCA EPPROM NV Logs", "BFh - PCA EEPROM Physical Calbration Table", "C0h - PCA-2 EPPROM KMA Security Table", "C1h - PCA-2 EPPROM Reserved"})
        Me.ComboBox2.Location = New System.Drawing.Point(128, 463)
        Me.ComboBox2.Name = "ComboBox2"
        Me.ComboBox2.Size = New System.Drawing.Size(770, 20)
        Me.ComboBox2.TabIndex = 42
        '
        'ButtonDebugDumpBuffer
        '
        Me.ButtonDebugDumpBuffer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugDumpBuffer.Location = New System.Drawing.Point(904, 462)
        Me.ButtonDebugDumpBuffer.Name = "ButtonDebugDumpBuffer"
        Me.ButtonDebugDumpBuffer.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugDumpBuffer.TabIndex = 41
        Me.ButtonDebugDumpBuffer.Text = "RAWDump"
        Me.ButtonDebugDumpBuffer.UseVisualStyleBackColor = True
        '
        'NumericUpDown7
        '
        Me.NumericUpDown7.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown7.Location = New System.Drawing.Point(838, 433)
        Me.NumericUpDown7.Maximum = New Decimal(New Integer() {524288, 0, 0, 0})
        Me.NumericUpDown7.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown7.Name = "NumericUpDown7"
        Me.NumericUpDown7.Size = New System.Drawing.Size(62, 21)
        Me.NumericUpDown7.TabIndex = 40
        Me.NumericUpDown7.Value = New Decimal(New Integer() {524288, 0, 0, 0})
        '
        'ButtonDebugReadBlock
        '
        Me.ButtonDebugReadBlock.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugReadBlock.Location = New System.Drawing.Point(904, 433)
        Me.ButtonDebugReadBlock.Name = "ButtonDebugReadBlock"
        Me.ButtonDebugReadBlock.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugReadBlock.TabIndex = 39
        Me.ButtonDebugReadBlock.Text = "Read Block"
        Me.ButtonDebugReadBlock.UseVisualStyleBackColor = True
        '
        'Label14
        '
        Me.Label14.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(9, 467)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(71, 12)
        Me.Label14.TabIndex = 38
        Me.Label14.Text = "Read Buffer"
        '
        'ButtonDebugRewind
        '
        Me.ButtonDebugRewind.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugRewind.Location = New System.Drawing.Point(128, 433)
        Me.ButtonDebugRewind.Name = "ButtonDebugRewind"
        Me.ButtonDebugRewind.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugRewind.TabIndex = 37
        Me.ButtonDebugRewind.Text = "Rewind"
        Me.ButtonDebugRewind.UseVisualStyleBackColor = True
        '
        'ButtonDebugDumpMAM
        '
        Me.ButtonDebugDumpMAM.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugDumpMAM.Location = New System.Drawing.Point(374, 489)
        Me.ButtonDebugDumpMAM.Name = "ButtonDebugDumpMAM"
        Me.ButtonDebugDumpMAM.Size = New System.Drawing.Size(92, 23)
        Me.ButtonDebugDumpMAM.TabIndex = 36
        Me.ButtonDebugDumpMAM.Text = "ReadAll"
        Me.ButtonDebugDumpMAM.UseVisualStyleBackColor = True
        '
        'ButtonDebugReadInfo
        '
        Me.ButtonDebugReadInfo.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugReadInfo.Location = New System.Drawing.Point(904, 491)
        Me.ButtonDebugReadInfo.Name = "ButtonDebugReadInfo"
        Me.ButtonDebugReadInfo.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugReadInfo.TabIndex = 35
        Me.ButtonDebugReadInfo.Text = "ReadInfo"
        Me.ButtonDebugReadInfo.UseVisualStyleBackColor = True
        '
        'ButtonDebugReadMAM
        '
        Me.ButtonDebugReadMAM.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugReadMAM.Location = New System.Drawing.Point(293, 489)
        Me.ButtonDebugReadMAM.Name = "ButtonDebugReadMAM"
        Me.ButtonDebugReadMAM.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugReadMAM.TabIndex = 34
        Me.ButtonDebugReadMAM.Text = "Read"
        Me.ButtonDebugReadMAM.UseVisualStyleBackColor = True
        '
        'NumericUpDown9
        '
        Me.NumericUpDown9.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown9.Location = New System.Drawing.Point(245, 491)
        Me.NumericUpDown9.Maximum = New Decimal(New Integer() {255, 0, 0, 0})
        Me.NumericUpDown9.Name = "NumericUpDown9"
        Me.NumericUpDown9.Size = New System.Drawing.Size(42, 21)
        Me.NumericUpDown9.TabIndex = 33
        Me.NumericUpDown9.Value = New Decimal(New Integer() {6, 0, 0, 0})
        '
        'NumericUpDown8
        '
        Me.NumericUpDown8.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown8.Location = New System.Drawing.Point(197, 491)
        Me.NumericUpDown8.Maximum = New Decimal(New Integer() {255, 0, 0, 0})
        Me.NumericUpDown8.Name = "NumericUpDown8"
        Me.NumericUpDown8.Size = New System.Drawing.Size(42, 21)
        Me.NumericUpDown8.TabIndex = 32
        Me.NumericUpDown8.Value = New Decimal(New Integer() {8, 0, 0, 0})
        '
        'Label15
        '
        Me.Label15.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(132, 493)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(59, 12)
        Me.Label15.TabIndex = 31
        Me.Label15.Text = "Page Code"
        '
        'Label13
        '
        Me.Label13.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(9, 493)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(77, 12)
        Me.Label13.TabIndex = 28
        Me.Label13.Text = "MAMAttribute"
        '
        'TextBox10
        '
        Me.TextBox10.Location = New System.Drawing.Point(128, 56)
        Me.TextBox10.Name = "TextBox10"
        Me.TextBox10.Size = New System.Drawing.Size(65, 21)
        Me.TextBox10.TabIndex = 27
        Me.TextBox10.Text = "2"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(75, 59)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(47, 12)
        Me.Label12.TabIndex = 26
        Me.Label12.Text = "datadir"
        '
        'ButtonDebugWriteBarcode
        '
        Me.ButtonDebugWriteBarcode.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugWriteBarcode.Location = New System.Drawing.Point(904, 521)
        Me.ButtonDebugWriteBarcode.Name = "ButtonDebugWriteBarcode"
        Me.ButtonDebugWriteBarcode.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugWriteBarcode.TabIndex = 25
        Me.ButtonDebugWriteBarcode.Text = "write"
        Me.ButtonDebugWriteBarcode.UseVisualStyleBackColor = True
        '
        'TextBox9
        '
        Me.TextBox9.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox9.Location = New System.Drawing.Point(412, 521)
        Me.TextBox9.MaxLength = 32
        Me.TextBox9.Name = "TextBox9"
        Me.TextBox9.Size = New System.Drawing.Size(485, 21)
        Me.TextBox9.TabIndex = 24
        Me.TextBox9.Text = "TEST00L5"
        '
        'Label11
        '
        Me.Label11.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(359, 526)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(47, 12)
        Me.Label11.TabIndex = 23
        Me.Label11.Text = "Barcode"
        '
        'Label10
        '
        Me.Label10.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(9, 526)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(83, 12)
        Me.Label10.TabIndex = 22
        Me.Label10.Text = "Partial Erase"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Font = New System.Drawing.Font("宋体", 9.0!, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.Label9.ForeColor = System.Drawing.Color.Blue
        Me.Label9.Location = New System.Drawing.Point(199, 59)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(29, 12)
        Me.Label9.TabIndex = 21
        Me.Label9.Text = "data"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(199, 34)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(23, 12)
        Me.Label8.TabIndex = 20
        Me.Label8.Text = "cdb"
        '
        'NumericUpDown6
        '
        Me.NumericUpDown6.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.NumericUpDown6.Location = New System.Drawing.Point(128, 521)
        Me.NumericUpDown6.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.NumericUpDown6.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown6.Name = "NumericUpDown6"
        Me.NumericUpDown6.Size = New System.Drawing.Size(55, 21)
        Me.NumericUpDown6.TabIndex = 19
        Me.NumericUpDown6.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'TextBox8
        '
        Me.TextBox8.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox8.Location = New System.Drawing.Point(128, 83)
        Me.TextBox8.MaxLength = 2147483647
        Me.TextBox8.Multiline = True
        Me.TextBox8.Name = "TextBox8"
        Me.TextBox8.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBox8.Size = New System.Drawing.Size(769, 344)
        Me.TextBox8.TabIndex = 18
        '
        'TextBox7
        '
        Me.TextBox7.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox7.Location = New System.Drawing.Point(228, 56)
        Me.TextBox7.MaxLength = 2147483647
        Me.TextBox7.Name = "TextBox7"
        Me.TextBox7.Size = New System.Drawing.Size(669, 21)
        Me.TextBox7.TabIndex = 17
        '
        'ButtonDebugErase
        '
        Me.ButtonDebugErase.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugErase.Location = New System.Drawing.Point(189, 521)
        Me.ButtonDebugErase.Name = "ButtonDebugErase"
        Me.ButtonDebugErase.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugErase.TabIndex = 16
        Me.ButtonDebugErase.Text = "erase"
        Me.ButtonDebugErase.UseVisualStyleBackColor = True
        '
        'ButtonDebugSendSCSICommand
        '
        Me.ButtonDebugSendSCSICommand.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDebugSendSCSICommand.Location = New System.Drawing.Point(904, 29)
        Me.ButtonDebugSendSCSICommand.Name = "ButtonDebugSendSCSICommand"
        Me.ButtonDebugSendSCSICommand.Size = New System.Drawing.Size(75, 23)
        Me.ButtonDebugSendSCSICommand.TabIndex = 15
        Me.ButtonDebugSendSCSICommand.Text = "send"
        Me.ButtonDebugSendSCSICommand.UseVisualStyleBackColor = True
        '
        'TextBox6
        '
        Me.TextBox6.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox6.Location = New System.Drawing.Point(228, 29)
        Me.TextBox6.Name = "TextBox6"
        Me.TextBox6.Size = New System.Drawing.Size(669, 21)
        Me.TextBox6.TabIndex = 14
        Me.TextBox6.Text = "1B 00 00 00 00 00"
        '
        'TextBox5
        '
        Me.TextBox5.Location = New System.Drawing.Point(128, 29)
        Me.TextBox5.Name = "TextBox5"
        Me.TextBox5.Size = New System.Drawing.Size(65, 21)
        Me.TextBox5.TabIndex = 13
        Me.TextBox5.Text = "\\.\TAPE0"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(9, 34)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(113, 12)
        Me.Label7.TabIndex = 12
        Me.Label7.Text = "_TapeSCSIIOCtlFull"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(3, 5)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(65, 12)
        Me.Label6.TabIndex = 8
        Me.Label6.Text = "奇怪的功能"
        '
        'SaveFileDialog1
        '
        Me.SaveFileDialog1.FileName = "MAMAttrib.xml"
        Me.SaveFileDialog1.Filter = "xml | *.xml"
        '
        'SaveFileDialog2
        '
        Me.SaveFileDialog2.FileName = "CM.bin"
        Me.SaveFileDialog2.Filter = "RAW Dump | *.bin"
        '
        'LTFSConfigurator
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1077, 662)
        Me.Controls.Add(Me.Panel1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "LTFSConfigurator"
        Me.Text = "LTFSConfigurator"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown7, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown8, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Button1 As Button
    Friend WithEvents ListBox1 As ListBox
    Friend WithEvents Button2 As Button
    Friend WithEvents Button3 As Button
    Friend WithEvents Button4 As Button
    Friend WithEvents ComboBox1 As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Button6 As Button
    Friend WithEvents Button7 As Button
    Friend WithEvents Label3 As Label
    Friend WithEvents Button8 As Button
    Friend WithEvents Button9 As Button
    Friend WithEvents Button10 As Button
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents Panel2 As Panel
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Label6 As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents TextBox6 As TextBox
    Friend WithEvents TextBox5 As TextBox
    Friend WithEvents ButtonDebugSendSCSICommand As Button
    Friend WithEvents Button13 As Button
    Friend WithEvents Button14 As Button
    Friend WithEvents ButtonDebugErase As Button
    Friend WithEvents TextBox7 As TextBox
    Friend WithEvents TextBox8 As TextBox
    Friend WithEvents NumericUpDown6 As NumericUpDown
    Friend WithEvents Label10 As Label
    Friend WithEvents Label9 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents ButtonDebugWriteBarcode As Button
    Friend WithEvents TextBox9 As TextBox
    Friend WithEvents Label11 As Label
    Friend WithEvents TextBox10 As TextBox
    Friend WithEvents Label12 As Label
    Friend WithEvents ButtonDebugReadMAM As Button
    Friend WithEvents NumericUpDown9 As NumericUpDown
    Friend WithEvents NumericUpDown8 As NumericUpDown
    Friend WithEvents Label15 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents ButtonDebugReadInfo As Button
    Friend WithEvents ButtonDebugDumpMAM As Button
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents Label14 As Label
    Friend WithEvents ButtonDebugRewind As Button
    Friend WithEvents SaveFileDialog2 As SaveFileDialog
    Friend WithEvents ButtonDebugReadBlock As Button
    Friend WithEvents NumericUpDown7 As NumericUpDown
    Friend WithEvents ButtonDebugDumpBuffer As Button
    Friend WithEvents ComboBox2 As ComboBox
    Friend WithEvents ButtonDebugLocate As Button
    Friend WithEvents Label16 As Label
    Friend WithEvents NumericUpDown1 As NumericUpDown
    Friend WithEvents NumericUpDown2 As NumericUpDown
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents ButtonDebugReadPosition As Button
    Friend WithEvents ButtonDebugDumpTape As Button
    Friend WithEvents FolderBrowserDialog1 As FolderBrowserDialog
    Friend WithEvents Button24 As Button
    Friend WithEvents CheckBox2 As CheckBox
    Friend WithEvents ComboBox3 As ComboBox
    Friend WithEvents ButtonDebugDumpIndex As Button
    Friend WithEvents ButtonDebugFormat As Button
    Friend WithEvents Button27 As Button
    Friend WithEvents ButtonDebugReleaseUnit As Button
    Friend WithEvents ButtonDebugAllowMediaRemoval As Button
    Friend WithEvents Button30 As Button
    Friend WithEvents CheckBox3 As CheckBox
End Class
