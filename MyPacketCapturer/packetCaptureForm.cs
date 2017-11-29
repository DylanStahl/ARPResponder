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
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace MyPacketCapturer
{
    public partial class packetCaptureForm : Form
    {
        private Button btnStartStop;
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
        private static int totalGoodput = 0;
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
        private ToolStripMenuItem exitToolStripMenuItem;
        public static string strPackets = "";
        private TextBox txtNumPackets;
        private Label label1;
        private ToolStripMenuItem packetsToolStripMenuItem;
        private ToolStripMenuItem sendWindowToolStripMenuItem;
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
        private TextBox udpGoodputTxtBox;
        private TextBox otherPacketCountTxtBox;
        private TextBox totalGoodputTxtBox;
        private Label label2;
        private Label label9;
        private Label label10;
        private TextBox otherGoodputTxtBox;
        private Label label11;
        private Label label12;
        private Button callForHelpBtn;
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

            //Select the proper device, even though we should probably do this at runtime. It's what's in the lab.
            device = devices[0];

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
                    callForHelpBtn.Enabled = true;
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packetsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtNumPackets = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.udpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.otherPacketCountTxtBox = new System.Windows.Forms.TextBox();
            this.totalGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.otherGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.callForHelpBtn = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.btnStartStop.Location = new System.Drawing.Point(503, 556);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 25);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "&Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
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
            this.packetsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(624, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.Click += new System.EventHandler(this.fileToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // packetsToolStripMenuItem
            // 
            this.packetsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendWindowToolStripMenuItem});
            this.packetsToolStripMenuItem.Name = "packetsToolStripMenuItem";
            this.packetsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.packetsToolStripMenuItem.Text = "&Packets";
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
            this.txtNumPackets.Location = new System.Drawing.Point(444, 428);
            this.txtNumPackets.Name = "txtNumPackets";
            this.txtNumPackets.Size = new System.Drawing.Size(134, 26);
            this.txtNumPackets.TabIndex = 4;
            this.txtNumPackets.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 431);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(186, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "Total Number Of Packets";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // icmpCntTxtBox
            // 
            this.icmpCntTxtBox.Location = new System.Drawing.Point(165, 129);
            this.icmpCntTxtBox.Name = "icmpCntTxtBox";
            this.icmpCntTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpCntTxtBox.TabIndex = 7;
            // 
            // icmpPktCntLabel
            // 
            this.icmpPktCntLabel.AutoSize = true;
            this.icmpPktCntLabel.Location = new System.Drawing.Point(48, 132);
            this.icmpPktCntLabel.Name = "icmpPktCntLabel";
            this.icmpPktCntLabel.Size = new System.Drawing.Size(95, 20);
            this.icmpPktCntLabel.TabIndex = 8;
            this.icmpPktCntLabel.Text = "ICMP Count";
            // 
            // icmpGoodputTxtBox
            // 
            this.icmpGoodputTxtBox.Location = new System.Drawing.Point(165, 168);
            this.icmpGoodputTxtBox.Name = "icmpGoodputTxtBox";
            this.icmpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpGoodputTxtBox.TabIndex = 9;
            // 
            // tcpCountTxtBox
            // 
            this.tcpCountTxtBox.Location = new System.Drawing.Point(478, 129);
            this.tcpCountTxtBox.Name = "tcpCountTxtBox";
            this.tcpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpCountTxtBox.TabIndex = 10;
            // 
            // tcpGoodputTxtBox
            // 
            this.tcpGoodputTxtBox.Location = new System.Drawing.Point(478, 168);
            this.tcpGoodputTxtBox.Name = "tcpGoodputTxtBox";
            this.tcpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpGoodputTxtBox.TabIndex = 11;
            // 
            // arpCountTxtBox
            // 
            this.arpCountTxtBox.Location = new System.Drawing.Point(165, 247);
            this.arpCountTxtBox.Name = "arpCountTxtBox";
            this.arpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpCountTxtBox.TabIndex = 12;
            // 
            // arpGoodputTxtBox
            // 
            this.arpGoodputTxtBox.Location = new System.Drawing.Point(165, 293);
            this.arpGoodputTxtBox.Name = "arpGoodputTxtBox";
            this.arpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpGoodputTxtBox.TabIndex = 13;
            // 
            // udpCountTxtBox
            // 
            this.udpCountTxtBox.Location = new System.Drawing.Point(478, 250);
            this.udpCountTxtBox.Name = "udpCountTxtBox";
            this.udpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpCountTxtBox.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 168);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 20);
            this.label3.TabIndex = 15;
            this.label3.Text = "ICMP Goodput";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(346, 132);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 20);
            this.label4.TabIndex = 16;
            this.label4.Text = "TCP Count";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(326, 168);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(106, 20);
            this.label5.TabIndex = 17;
            this.label5.Text = "TCP Goodput";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(48, 247);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 20);
            this.label6.TabIndex = 18;
            this.label6.Text = "ARP Count";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(29, 293);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(109, 20);
            this.label7.TabIndex = 19;
            this.label7.Text = "ARP Goodput";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(342, 253);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 20);
            this.label8.TabIndex = 20;
            this.label8.Text = "UDP Count";
            // 
            // udpGoodputTxtBox
            // 
            this.udpGoodputTxtBox.Location = new System.Drawing.Point(478, 293);
            this.udpGoodputTxtBox.Name = "udpGoodputTxtBox";
            this.udpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpGoodputTxtBox.TabIndex = 21;
            // 
            // otherPacketCountTxtBox
            // 
            this.otherPacketCountTxtBox.Location = new System.Drawing.Point(444, 356);
            this.otherPacketCountTxtBox.Name = "otherPacketCountTxtBox";
            this.otherPacketCountTxtBox.Size = new System.Drawing.Size(134, 26);
            this.otherPacketCountTxtBox.TabIndex = 22;
            // 
            // totalGoodputTxtBox
            // 
            this.totalGoodputTxtBox.Location = new System.Drawing.Point(444, 463);
            this.totalGoodputTxtBox.Name = "totalGoodputTxtBox";
            this.totalGoodputTxtBox.Size = new System.Drawing.Size(134, 26);
            this.totalGoodputTxtBox.TabIndex = 23;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(322, 296);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 20);
            this.label2.TabIndex = 24;
            this.label2.Text = "UDP Goodput";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(48, 466);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(263, 20);
            this.label9.TabIndex = 25;
            this.label9.Text = "Total Goodput of Observed Network";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(48, 359);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(197, 20);
            this.label10.TabIndex = 26;
            this.label10.Text = "Other Traffic Packet Count";
            // 
            // otherGoodputTxtBox
            // 
            this.otherGoodputTxtBox.Location = new System.Drawing.Point(444, 392);
            this.otherGoodputTxtBox.Name = "otherGoodputTxtBox";
            this.otherGoodputTxtBox.Size = new System.Drawing.Size(134, 26);
            this.otherGoodputTxtBox.TabIndex = 27;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(48, 395);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(164, 20);
            this.label11.TabIndex = 28;
            this.label11.Text = "Other Traffic Goodput";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F);
            this.label12.Location = new System.Drawing.Point(47, 55);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(170, 26);
            this.label12.TabIndex = 29;
            this.label12.Text = "ARP Responder";
            this.label12.UseMnemonic = false;
            this.label12.Click += new System.EventHandler(this.label12_Click);
            // 
            // callForHelpBtn
            // 
            this.callForHelpBtn.Enabled = false;
            this.callForHelpBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.callForHelpBtn.Location = new System.Drawing.Point(413, 556);
            this.callForHelpBtn.Name = "callForHelpBtn";
            this.callForHelpBtn.Size = new System.Drawing.Size(84, 25);
            this.callForHelpBtn.TabIndex = 30;
            this.callForHelpBtn.Text = "&Call for Help";
            this.callForHelpBtn.UseVisualStyleBackColor = true;
            this.callForHelpBtn.Click += new System.EventHandler(this.callForHelpBtn_Click);
            // 
            // packetCaptureForm
            // 
            this.ClientSize = new System.Drawing.Size(624, 593);
            this.Controls.Add(this.callForHelpBtn);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.otherGoodputTxtBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.totalGoodputTxtBox);
            this.Controls.Add(this.otherPacketCountTxtBox);
            this.Controls.Add(this.udpGoodputTxtBox);
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
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtNumPackets);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "packetCaptureForm";
            this.Text = " ARP Responder";
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
            totalGoodput += icmpThroughput + arpThroughput + tcpThroughput + otherThroughput;
            totalGoodput -= icmpOverhead + arpOverhead + tcpOverhead + otherOverhead;

            strPackets = "";
            txtNumPackets.Text = Convert.ToString(numPackets);
            icmpCntTxtBox.Text = Convert.ToString(icmpPacketsReceived);
            icmpGoodputTxtBox.Text = Convert.ToString((icmpThroughput - icmpOverhead));
            tcpCountTxtBox.Text = Convert.ToString(tcpPacketsReceived);
            tcpGoodputTxtBox.Text = Convert.ToString(tcpThroughput - tcpOverhead);
            arpCountTxtBox.Text = Convert.ToString(arpPacketsReceived);
            arpGoodputTxtBox.Text = Convert.ToString(arpThroughput - arpOverhead);
            udpCountTxtBox.Text = Convert.ToString(udpPacketsReceived);
            udpGoodputTxtBox.Text = Convert.ToString(udpThroughput - udpOverhead);
            otherPacketCountTxtBox.Text = Convert.ToString(otherPackets);
            otherGoodputTxtBox.Text = Convert.ToString(otherThroughput - otherOverhead);
            totalGoodputTxtBox.Text = Convert.ToString(totalGoodput);
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {

            //We need a handler. Let's fix that.
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            //Create timeout and then open the device for capturing.
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
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

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void callForHelpBtn_Click(object sender, EventArgs e)
        {
            Wallpaper.Set();
        }
    }
}

public sealed class Wallpaper
{
    Wallpaper() { }

    const int SPI_SETDESKWALLPAPER = 20;
    const int SPIF_UPDATEINIFILE = 0x01;
    const int SPIF_SENDWININICHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    

    public static void Set()
    {

        System.Drawing.Image img = System.Drawing.Image.FromFile("stalin.jpg");
        string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");
        img.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);

        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
        key.SetValue(@"WallpaperStyle", 1.ToString());
        key.SetValue(@"TileWallpaper", 0.ToString());

        SystemParametersInfo(SPI_SETDESKWALLPAPER,
            0,
            tempPath,
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
    }
}
