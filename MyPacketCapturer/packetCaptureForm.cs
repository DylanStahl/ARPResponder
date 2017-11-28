using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PacketDotNet;
using SharpPcap;

namespace MyPacketCapturer
{
    public partial class packetCaptureForm : Form
    {
        private Button btnStartStop;
        private ComboBox cmbDevices;
        private TextBox txtCapturedData;
        static int numPackets = 0;
        private static int IP_HEADER_LENGTH = 0;
        private static int tcpPacketsReceived = 0;
        private static int tcpThroughput = 0;
        private static int tcpOverhead = 0;
        private static int udpPacketsReceived = 0;
        private static int udpThroughput = 0;
        private static int udpOverhead = 0;
        private static int icmpPacketsReceived = 0;
        private static int icmpThroughput = 0;
        private static int icmpOverhead = 0;
        private static int arpPacketsReceived = 0;
        private static int arpThroughput = 0;
        private static int arpOverhead = 0;
        private static int otherPackets = 0;
        private static int otherThroughput = 0;
        private static int otherOverhead = 0;
        private static int gratuitousArps = 0;
        private static DateTime timestampOfLastARPRequest;

        sendPacketForm fSend;

        //Variables use to house network capture devices available, the selected device, and the packet string.
        CaptureDeviceList devices;
        public static ICaptureDevice device;
        private Timer timer1;
        private IContainer components;
        private OpenFileDialog openFileDialog1;
        private SaveFileDialog saveFileDialog1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem screenToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        public static string strPackets = "";
        private TextBox txtNumPackets;
        private Label label1;
        private ToolStripMenuItem packetsToolStripMenuItem;
        private ToolStripMenuItem sendWindowToolStripMenuItem;
        private TextBox txtGUID;
        private TextBox icmpCntTxtBox;
        private Label icmpPktCntLabel;
        private TextBox icmpGoodputTxtBox;
        private TextBox tcpCountTxtBox;
        private TextBox tcpGoodputTxtBox;
        private TextBox arpCountTxtBox;
        private TextBox arpGoodputTxtBox;
        private TextBox udpCountTxtBox;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private bool captureHasBeenSaved = true;

        public packetCaptureForm()
        {
            //Initialize the Form, and then grab the device instances.
            InitializeComponent();
            devices = CaptureDeviceList.Instance;

            //Make sure devices exist in the list.
            if (devices.Count < 1) {
                MessageBox.Show("No devices found. Aw nuts.");
                Application.Exit();
            }

            //Add devices to combo box.
            foreach (ICaptureDevice dev in devices) {
                cmbDevices.Items.Add(dev.Description);
            }

            //Select the proper device, even though we should probably do this at runtime. It's what's in the lab.
            device = devices[0];
            cmbDevices.Text = device.ToString();

            //We need a handler. Let's fix that.
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            //Create timeout and then open the device for capturing.
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

        }

        private static void device_OnPacketArrival(object sender, CaptureEventArgs packet) {

            //Increment packet count
            numPackets++;

            //Put the packet number in the capture window
            strPackets += "Packet Number: " + Convert.ToString(numPackets) + Environment.NewLine;

            //Array for data storage.
            byte[] data = packet.Packet.Data;

            IP_HEADER_LENGTH = data[14] & 15;

            //Keep track of the number of bytes displayed per line
            int byteCounter = 0;

            strPackets += "Destination MAC Address: ";
            //Parse each packet.
            foreach (byte datum in data)
            {
                //Add byte to our string (in hex)
                if (byteCounter <= 13)
                {
                    strPackets += datum.ToString("X2") + " ";
                    byteCounter++;

                    switch (byteCounter) {
                        case 6:
                            strPackets += Environment.NewLine + "Source MAC Address: ";
                            break;
                        case 12:
                            strPackets += Environment.NewLine + "EtherType: ";
                            break;
                        case 14:
                            if (data[12] == 8) {
                                if (data[13] == 0) {
                                    strPackets += "(IP)";
                                    switch (data[23])
                                    {
                                        case 1:
                                            icmpPacketsReceived += 1;
                                            icmpThroughput += data[16] * 256 + data[17];
                                            icmpOverhead += IP_HEADER_LENGTH;
                                            break;
                                        case 6:
                                            tcpPacketsReceived += 1;
                                            tcpThroughput += data[16] * 256 + data[17];
                                            tcpOverhead += IP_HEADER_LENGTH;
                                            break;
                                        case 17:
                                            udpPacketsReceived += 1;
                                            udpThroughput += data[16] * 256 + data[17];
                                            udpOverhead += IP_HEADER_LENGTH;
                                            break;
                                        default:
                                            otherPackets += 1;
                                            otherThroughput += data[16] * 256 + data[17];
                                            otherOverhead += IP_HEADER_LENGTH;
                                            break;
                }
                                }
                                if (data[13] == 6)
                                {
                                    strPackets += "(ARP)";
                                    arpPacketsReceived += 1;
                                    arpThroughput += 28;
                                    arpOverhead += 8;
                                    if(data[21] == 1){
                                        timestampOfLastARPRequest = DateTime.Now.Date;
                                    }
                                    if(data[21] == 2){
                                        if (timestampOfLastARPRequest > DateTime.Now.AddSeconds(2)){
                                            //We've encountered a gratuitous ARP.
                                            //Call Cthulhu.
                                            gratuitousArps += 1;
                                        }
                                    }
                                }
                            }
                            strPackets += Environment.NewLine;
                            break;
                        default:
                            break;
                    }
                }



            }

            //Reset Byte Count, and inform the user of non-parsed packet data.
            byteCounter = 0;
            strPackets += Environment.NewLine + Environment.NewLine + "Raw Data" + Environment.NewLine;

            //Process each byte.
            foreach (byte datum in data) {
                //Add byte to our string (in hex)
                strPackets += datum.ToString("X2") + " ";
                byteCounter++;
                if (byteCounter == 16) {
                    byteCounter = 0;
                    strPackets += Environment.NewLine; //... \n?
                }
            }
            strPackets += Environment.NewLine;
            strPackets += Environment.NewLine;
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStartStop.Text == "Start")
                {
                    device.StartCapture();
                    timer1.Enabled = true;
                    captureHasBeenSaved = false;
                    btnStartStop.Text = "Stop";
                } else {
                    device.StopCapture();
                    timer1.Enabled = false;
                    btnStartStop.Text = "Start";
                }
            }
            catch (Exception ex) {
                MessageBox.Show("We caught an exception. It's a biggun!" + ex.ToString());
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.txtCapturedData = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packetsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtNumPackets = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtGUID = new System.Windows.Forms.TextBox();
            this.icmpCntTxtBox = new System.Windows.Forms.TextBox();
            this.icmpPktCntLabel = new System.Windows.Forms.Label();
            this.icmpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.tcpCountTxtBox = new System.Windows.Forms.TextBox();
            this.tcpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.arpCountTxtBox = new System.Windows.Forms.TextBox();
            this.arpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.udpCountTxtBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(70, 51);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(158, 45);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // cmbDevices
            // 
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Location = new System.Drawing.Point(70, 129);
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.Size = new System.Drawing.Size(760, 28);
            this.cmbDevices.TabIndex = 1;
            this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
            // 
            // txtCapturedData
            // 
            this.txtCapturedData.Location = new System.Drawing.Point(70, 178);
            this.txtCapturedData.Multiline = true;
            this.txtCapturedData.Name = "txtCapturedData";
            this.txtCapturedData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCapturedData.Size = new System.Drawing.Size(760, 339);
            this.txtCapturedData.TabIndex = 2;
            this.txtCapturedData.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.screenToolStripMenuItem,
            this.packetsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1098, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            this.fileToolStripMenuItem.Click += new System.EventHandler(this.fileToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // screenToolStripMenuItem
            // 
            this.screenToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem});
            this.screenToolStripMenuItem.Name = "screenToolStripMenuItem";
            this.screenToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.screenToolStripMenuItem.Text = "Screen";
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(101, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            // 
            // packetsToolStripMenuItem
            // 
            this.packetsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendWindowToolStripMenuItem});
            this.packetsToolStripMenuItem.Name = "packetsToolStripMenuItem";
            this.packetsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.packetsToolStripMenuItem.Text = "Packets";
            // 
            // sendWindowToolStripMenuItem
            // 
            this.sendWindowToolStripMenuItem.Name = "sendWindowToolStripMenuItem";
            this.sendWindowToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.sendWindowToolStripMenuItem.Text = "&Send Window";
            this.sendWindowToolStripMenuItem.Click += new System.EventHandler(this.sendWindowToolStripMenuItem_Click);
            // 
            // txtNumPackets
            // 
            this.txtNumPackets.Location = new System.Drawing.Point(681, 63);
            this.txtNumPackets.Name = "txtNumPackets";
            this.txtNumPackets.Size = new System.Drawing.Size(149, 26);
            this.txtNumPackets.TabIndex = 4;
            this.txtNumPackets.Text = "0";
            this.txtNumPackets.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(528, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(147, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "Number Of Packets";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtGUID
            // 
            this.txtGUID.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtGUID.Location = new System.Drawing.Point(70, 529);
            this.txtGUID.Name = "txtGUID";
            this.txtGUID.Size = new System.Drawing.Size(760, 19);
            this.txtGUID.TabIndex = 6;
            // 
            // icmpCntTxtBox
            // 
            this.icmpCntTxtBox.Location = new System.Drawing.Point(970, 60);
            this.icmpCntTxtBox.Name = "icmpCntTxtBox";
            this.icmpCntTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpCntTxtBox.TabIndex = 7;
            // 
            // icmpPktCntLabel
            // 
            this.icmpPktCntLabel.AutoSize = true;
            this.icmpPktCntLabel.Location = new System.Drawing.Point(853, 63);
            this.icmpPktCntLabel.Name = "icmpPktCntLabel";
            this.icmpPktCntLabel.Size = new System.Drawing.Size(95, 20);
            this.icmpPktCntLabel.TabIndex = 8;
            this.icmpPktCntLabel.Text = "ICMP Count";
            // 
            // icmpGoodputTxtBox
            // 
            this.icmpGoodputTxtBox.Location = new System.Drawing.Point(970, 93);
            this.icmpGoodputTxtBox.Name = "icmpGoodputTxtBox";
            this.icmpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpGoodputTxtBox.TabIndex = 9;
            // 
            // tcpCountTxtBox
            // 
            this.tcpCountTxtBox.Location = new System.Drawing.Point(970, 129);
            this.tcpCountTxtBox.Name = "tcpCountTxtBox";
            this.tcpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpCountTxtBox.TabIndex = 10;
            // 
            // tcpGoodputTxtBox
            // 
            this.tcpGoodputTxtBox.Location = new System.Drawing.Point(970, 162);
            this.tcpGoodputTxtBox.Name = "tcpGoodputTxtBox";
            this.tcpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpGoodputTxtBox.TabIndex = 11;
            // 
            // arpCountTxtBox
            // 
            this.arpCountTxtBox.Location = new System.Drawing.Point(970, 194);
            this.arpCountTxtBox.Name = "arpCountTxtBox";
            this.arpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpCountTxtBox.TabIndex = 12;
            // 
            // arpGoodputTxtBox
            // 
            this.arpGoodputTxtBox.Location = new System.Drawing.Point(970, 227);
            this.arpGoodputTxtBox.Name = "arpGoodputTxtBox";
            this.arpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpGoodputTxtBox.TabIndex = 13;
            // 
            // udpCountTxtBox
            // 
            this.udpCountTxtBox.Location = new System.Drawing.Point(970, 260);
            this.udpCountTxtBox.Name = "udpCountTxtBox";
            this.udpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpCountTxtBox.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(849, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 20);
            this.label3.TabIndex = 15;
            this.label3.Text = "ICMP Goodput";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(849, 132);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 20);
            this.label4.TabIndex = 16;
            this.label4.Text = "TCP Count";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(849, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(106, 20);
            this.label5.TabIndex = 17;
            this.label5.Text = "TCP Goodput";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(858, 197);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 20);
            this.label6.TabIndex = 18;
            this.label6.Text = "ARP Count";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(855, 230);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(109, 20);
            this.label7.TabIndex = 19;
            this.label7.Text = "ARP Goodput";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(858, 263);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 20);
            this.label8.TabIndex = 20;
            this.label8.Text = "UDP Count";
            // 
            // packetCaptureForm
            // 
            this.ClientSize = new System.Drawing.Size(1098, 561);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.udpCountTxtBox);
            this.Controls.Add(this.arpGoodputTxtBox);
            this.Controls.Add(this.arpCountTxtBox);
            this.Controls.Add(this.tcpGoodputTxtBox);
            this.Controls.Add(this.tcpCountTxtBox);
            this.Controls.Add(this.icmpGoodputTxtBox);
            this.Controls.Add(this.icmpPktCntLabel);
            this.Controls.Add(this.icmpCntTxtBox);
            this.Controls.Add(this.txtGUID);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtNumPackets);
            this.Controls.Add(this.txtCapturedData);
            this.Controls.Add(this.cmbDevices);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "packetCaptureForm";
            this.Text = " ";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            txtCapturedData.AppendText(strPackets);
            strPackets = "";
            txtNumPackets.Text = Convert.ToString(numPackets);
            icmpCntTxtBox.Text = Convert.ToString(icmpPacketsReceived);
            icmpGoodputTxtBox.Text = Convert.ToString((icmpThroughput - icmpOverhead));
            tcpCountTxtBox.Text = Convert.ToString(tcpPacketsReceived);
            tcpGoodputTxtBox.Text = Convert.ToString(tcpThroughput - tcpOverhead);
            arpCountTxtBox.Text = Convert.ToString(arpPacketsReceived);
            arpGoodputTxtBox.Text = Convert.ToString(arpThroughput - arpOverhead);
            udpCountTxtBox.Text = Convert.ToString(udpPacketsReceived);
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            device = devices[cmbDevices.SelectedIndex];
            cmbDevices.Text = device.Description;
            txtGUID.Text = device.Name;

            //We need a handler. Let's fix that.
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            //Create timeout and then open the device for capturing.
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|All Files| *.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCapturedData.Text);
                captureHasBeenSaved = true;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|All Files| *.*";
            openFileDialog1.Title = "Open Captured Packets";
            openFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (openFileDialog1.FileName != "" && captureHasBeenSaved)
            {
                txtCapturedData.Text = "";
                txtCapturedData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
            else
            {
                DateTime currentDate = DateTime.Now;

                System.IO.File.WriteAllText("Capture_" + currentDate.ToShortDateString() +".txt", txtCapturedData.Text);
                txtCapturedData.Text = "";
                txtCapturedData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Check if packet capture has been saved
            if (!captureHasBeenSaved)
            {
                //If not, quit.
                Application.Exit();
            }
            else {
                //If so, ask if they want to save before quitting.
                MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;
                string title = "Unsaved Capture";
                string message = "You have not saved this capture: save capture?";
                DialogResult result;

                //Display message box to warn user of unsaved packets.
                result = MessageBox.Show(message, title, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    this.Close();

                    saveFileDialog1.Filter = "Text Files|*.txt|All Files| *.*";
                    saveFileDialog1.Title = "Save the Captured Packets";
                    saveFileDialog1.ShowDialog();

                    //Check to see if filename was given
                    if (saveFileDialog1.FileName != "")
                    {
                        System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCapturedData.Text);
                        captureHasBeenSaved = true;
                        Application.Exit();
                    }
                    else
                    {
                        DateTime currentDate = DateTime.Now;

                        System.IO.File.WriteAllText(saveFileDialog1.FileName + "Capture_" + (String)currentDate.ToLongDateString() + ".txt", txtCapturedData.Text);
                        captureHasBeenSaved = true;
                        Application.Exit();
                    }
                }
                else if (result == System.Windows.Forms.DialogResult.No)
                {

                    Application.Exit();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void sendWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sendPacketForm.instantiations == 0)
            {
                fSend = new sendPacketForm();
                fSend.Show();
            }
            else {
                fSend.BringToFront();
                fSend.Focus();
            }
        }
    }
}
