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
using System.Diagnostics;
using System.Media;

namespace MyPacketCapturer
{
    public partial class packetCaptureForm : Form
    {
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

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
        private static Stopwatch arptimer = new Stopwatch();
        private static bool soundPlayed = false;
        private static SoundPlayer soundPlayer;
        private static int wallpaperChoice = -1;
        private static int negativeOffset = 0;
        private static float hundredMultiplier = 1f;


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
        private TextBox txtNumPackets;
        private Label totalPacketsLabel;
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
        private Label icmpGoodputLabel;
        private Label tcpCountLabel;
        private Label tcpGoodputLabel;
        private Label arpCountLabel;
        private Label arpGoodputLabel;
        private Label udpCountLabel;
        private TextBox udpGoodputTxtBox;
        private TextBox otherPacketCountTxtBox;
        private TextBox totalGoodputTxtBox;
        private Label udpGoodputLabel;
        private Label totalGoodputLabel;
        private Label otherCountLabel;
        private TextBox otherGoodputTxtBox;
        private Label otherGoodputLabel;
        private Label arpRespTitle;
        private Button callForHelpBtn;
        private Label icmpThroughputLabel;
        private TextBox icmpThroughputTxtBox;
        private TextBox tcpThroughputTxtBox;
        private Label tcpThroughputLabel;
        private TextBox arpThroughputTxtBox;
        private TextBox udpThroughputTxtBox;
        private Label arpThroughputLabel;
        private ProgressBar gratArp;
        private Label udpThroughputLabel;

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

            //waveOutSetVolume(IntPtr.Zero, (uint)0);

            soundPlayer = new SoundPlayer();
            soundPlayer.SoundLocation = "soviet-anthem.wav";
            soundPlayer.Load();
            soundPlayer.Play();
            
            
        }
        

        private static void device_OnPacketArrival(object sender, CaptureEventArgs packet) {

            //Increment packet count
            numPackets += 1;

            //Array for data storage.
            byte[] data = packet.Packet.Data;

            IP_HEADER_LENGTH = data[14] & 15;


            if (data[12] == 8)
            {
                if (data[13] == 0)
                {
                    switch (data[23])
                    {
                        case 1:
                            icmpPacketsReceived += 1;
                            totalGoodput += data[16] * 256 + data[17] - (IP_HEADER_LENGTH * 4);
                            icmpThroughput += data[16] * 256 + data[17];
                            icmpOverhead += IP_HEADER_LENGTH * 4;
                            break;
                        case 6:
                            tcpPacketsReceived += 1;
                            totalGoodput += data[16] * 256 + data[17] - (IP_HEADER_LENGTH * 4);
                            tcpThroughput += data[16] * 256 + data[17];
                            tcpOverhead += IP_HEADER_LENGTH * 4;
                            break;
                        case 17:
                            udpPacketsReceived += 1;
                            totalGoodput += data[16] * 256 + data[17] - (IP_HEADER_LENGTH * 4);
                            udpThroughput += data[16] * 256 + data[17];
                            udpOverhead += IP_HEADER_LENGTH * 4;
                            break;
                        default:
                            otherPackets += 1;
                            totalGoodput += data[16] * 256 + data[17] - (IP_HEADER_LENGTH * 4);
                            otherThroughput += data[16] * 256 + data[17];
                            otherOverhead += IP_HEADER_LENGTH * 4;
                            break;
                    }
                }
                if (data[13] == 6)
                {
                    arpPacketsReceived += 1;
                    totalGoodput += 20;
                    arpThroughput += 28;
                    arpOverhead += 8;
                    if (data[21] == 1)
                    {
                        if (arptimer.IsRunning)
                        {
                            arptimer.Restart();
                        }
                        else
                        {
                            arptimer.Start();
                        }
                    }
                    if (data[21] == 2)
                    {
                        if (!arptimer.IsRunning)
                        {
                            gratuitousArps += 1;
                            if (!soundPlayed)
                            {
                                Console.WriteLine("Sound should be played.");
                                soundPlayer.Play();
                                soundPlayed = true;
                            }
                        }
                        else if (arptimer.ElapsedMilliseconds > 2000) {
                            gratuitousArps += 1;

                            if (!soundPlayed)
                            {
                                Console.WriteLine("Sound should be played.");
                                soundPlayer.Play();
                                soundPlayed = true;
                            }
                        }
                    }
                }
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStartStop.Text == "Start")
                {
                    device.StartCapture();
                    timer1.Enabled = true;
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
            this.totalPacketsLabel = new System.Windows.Forms.Label();
            this.icmpCntTxtBox = new System.Windows.Forms.TextBox();
            this.icmpPktCntLabel = new System.Windows.Forms.Label();
            this.icmpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.tcpCountTxtBox = new System.Windows.Forms.TextBox();
            this.tcpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.arpCountTxtBox = new System.Windows.Forms.TextBox();
            this.arpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.udpCountTxtBox = new System.Windows.Forms.TextBox();
            this.icmpGoodputLabel = new System.Windows.Forms.Label();
            this.tcpCountLabel = new System.Windows.Forms.Label();
            this.tcpGoodputLabel = new System.Windows.Forms.Label();
            this.arpCountLabel = new System.Windows.Forms.Label();
            this.arpGoodputLabel = new System.Windows.Forms.Label();
            this.udpCountLabel = new System.Windows.Forms.Label();
            this.udpGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.otherPacketCountTxtBox = new System.Windows.Forms.TextBox();
            this.totalGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.udpGoodputLabel = new System.Windows.Forms.Label();
            this.totalGoodputLabel = new System.Windows.Forms.Label();
            this.otherCountLabel = new System.Windows.Forms.Label();
            this.otherGoodputTxtBox = new System.Windows.Forms.TextBox();
            this.otherGoodputLabel = new System.Windows.Forms.Label();
            this.arpRespTitle = new System.Windows.Forms.Label();
            this.callForHelpBtn = new System.Windows.Forms.Button();
            this.icmpThroughputLabel = new System.Windows.Forms.Label();
            this.icmpThroughputTxtBox = new System.Windows.Forms.TextBox();
            this.tcpThroughputTxtBox = new System.Windows.Forms.TextBox();
            this.tcpThroughputLabel = new System.Windows.Forms.Label();
            this.arpThroughputTxtBox = new System.Windows.Forms.TextBox();
            this.udpThroughputTxtBox = new System.Windows.Forms.TextBox();
            this.arpThroughputLabel = new System.Windows.Forms.Label();
            this.udpThroughputLabel = new System.Windows.Forms.Label();
            this.gratArp = new System.Windows.Forms.ProgressBar();
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
            this.txtNumPackets.Enabled = false;
            this.txtNumPackets.Location = new System.Drawing.Point(444, 459);
            this.txtNumPackets.Name = "txtNumPackets";
            this.txtNumPackets.Size = new System.Drawing.Size(134, 26);
            this.txtNumPackets.TabIndex = 4;
            this.txtNumPackets.Text = "0";
            // 
            // totalPacketsLabel
            // 
            this.totalPacketsLabel.AutoSize = true;
            this.totalPacketsLabel.Location = new System.Drawing.Point(48, 462);
            this.totalPacketsLabel.Name = "totalPacketsLabel";
            this.totalPacketsLabel.Size = new System.Drawing.Size(186, 20);
            this.totalPacketsLabel.TabIndex = 5;
            this.totalPacketsLabel.Text = "Total Number Of Packets";
            this.totalPacketsLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // icmpCntTxtBox
            // 
            this.icmpCntTxtBox.Enabled = false;
            this.icmpCntTxtBox.Location = new System.Drawing.Point(165, 113);
            this.icmpCntTxtBox.Name = "icmpCntTxtBox";
            this.icmpCntTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpCntTxtBox.TabIndex = 7;
            // 
            // icmpPktCntLabel
            // 
            this.icmpPktCntLabel.AutoSize = true;
            this.icmpPktCntLabel.Location = new System.Drawing.Point(48, 116);
            this.icmpPktCntLabel.Name = "icmpPktCntLabel";
            this.icmpPktCntLabel.Size = new System.Drawing.Size(95, 20);
            this.icmpPktCntLabel.TabIndex = 8;
            this.icmpPktCntLabel.Text = "ICMP Count";
            // 
            // icmpGoodputTxtBox
            // 
            this.icmpGoodputTxtBox.Enabled = false;
            this.icmpGoodputTxtBox.Location = new System.Drawing.Point(165, 144);
            this.icmpGoodputTxtBox.Name = "icmpGoodputTxtBox";
            this.icmpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpGoodputTxtBox.TabIndex = 9;
            // 
            // tcpCountTxtBox
            // 
            this.tcpCountTxtBox.Enabled = false;
            this.tcpCountTxtBox.Location = new System.Drawing.Point(478, 113);
            this.tcpCountTxtBox.Name = "tcpCountTxtBox";
            this.tcpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpCountTxtBox.TabIndex = 10;
            // 
            // tcpGoodputTxtBox
            // 
            this.tcpGoodputTxtBox.Enabled = false;
            this.tcpGoodputTxtBox.Location = new System.Drawing.Point(478, 144);
            this.tcpGoodputTxtBox.Name = "tcpGoodputTxtBox";
            this.tcpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpGoodputTxtBox.TabIndex = 11;
            // 
            // arpCountTxtBox
            // 
            this.arpCountTxtBox.Enabled = false;
            this.arpCountTxtBox.Location = new System.Drawing.Point(165, 247);
            this.arpCountTxtBox.Name = "arpCountTxtBox";
            this.arpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpCountTxtBox.TabIndex = 12;
            // 
            // arpGoodputTxtBox
            // 
            this.arpGoodputTxtBox.Enabled = false;
            this.arpGoodputTxtBox.Location = new System.Drawing.Point(165, 279);
            this.arpGoodputTxtBox.Name = "arpGoodputTxtBox";
            this.arpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpGoodputTxtBox.TabIndex = 13;
            // 
            // udpCountTxtBox
            // 
            this.udpCountTxtBox.Enabled = false;
            this.udpCountTxtBox.Location = new System.Drawing.Point(478, 250);
            this.udpCountTxtBox.Name = "udpCountTxtBox";
            this.udpCountTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpCountTxtBox.TabIndex = 14;
            // 
            // icmpGoodputLabel
            // 
            this.icmpGoodputLabel.AutoSize = true;
            this.icmpGoodputLabel.Location = new System.Drawing.Point(28, 147);
            this.icmpGoodputLabel.Name = "icmpGoodputLabel";
            this.icmpGoodputLabel.Size = new System.Drawing.Size(115, 20);
            this.icmpGoodputLabel.TabIndex = 15;
            this.icmpGoodputLabel.Text = "ICMP Goodput";
            // 
            // tcpCountLabel
            // 
            this.tcpCountLabel.AutoSize = true;
            this.tcpCountLabel.Location = new System.Drawing.Point(361, 116);
            this.tcpCountLabel.Name = "tcpCountLabel";
            this.tcpCountLabel.Size = new System.Drawing.Size(86, 20);
            this.tcpCountLabel.TabIndex = 16;
            this.tcpCountLabel.Text = "TCP Count";
            // 
            // tcpGoodputLabel
            // 
            this.tcpGoodputLabel.AutoSize = true;
            this.tcpGoodputLabel.Location = new System.Drawing.Point(342, 147);
            this.tcpGoodputLabel.Name = "tcpGoodputLabel";
            this.tcpGoodputLabel.Size = new System.Drawing.Size(106, 20);
            this.tcpGoodputLabel.TabIndex = 17;
            this.tcpGoodputLabel.Text = "TCP Goodput";
            // 
            // arpCountLabel
            // 
            this.arpCountLabel.AutoSize = true;
            this.arpCountLabel.Location = new System.Drawing.Point(48, 247);
            this.arpCountLabel.Name = "arpCountLabel";
            this.arpCountLabel.Size = new System.Drawing.Size(89, 20);
            this.arpCountLabel.TabIndex = 18;
            this.arpCountLabel.Text = "ARP Count";
            // 
            // arpGoodputLabel
            // 
            this.arpGoodputLabel.AutoSize = true;
            this.arpGoodputLabel.Location = new System.Drawing.Point(28, 282);
            this.arpGoodputLabel.Name = "arpGoodputLabel";
            this.arpGoodputLabel.Size = new System.Drawing.Size(109, 20);
            this.arpGoodputLabel.TabIndex = 19;
            this.arpGoodputLabel.Text = "ARP Goodput";
            // 
            // udpCountLabel
            // 
            this.udpCountLabel.AutoSize = true;
            this.udpCountLabel.Location = new System.Drawing.Point(357, 253);
            this.udpCountLabel.Name = "udpCountLabel";
            this.udpCountLabel.Size = new System.Drawing.Size(90, 20);
            this.udpCountLabel.TabIndex = 20;
            this.udpCountLabel.Text = "UDP Count";
            // 
            // udpGoodputTxtBox
            // 
            this.udpGoodputTxtBox.Enabled = false;
            this.udpGoodputTxtBox.Location = new System.Drawing.Point(478, 282);
            this.udpGoodputTxtBox.Name = "udpGoodputTxtBox";
            this.udpGoodputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpGoodputTxtBox.TabIndex = 21;
            // 
            // otherPacketCountTxtBox
            // 
            this.otherPacketCountTxtBox.Enabled = false;
            this.otherPacketCountTxtBox.Location = new System.Drawing.Point(444, 389);
            this.otherPacketCountTxtBox.Name = "otherPacketCountTxtBox";
            this.otherPacketCountTxtBox.Size = new System.Drawing.Size(134, 26);
            this.otherPacketCountTxtBox.TabIndex = 22;
            // 
            // totalGoodputTxtBox
            // 
            this.totalGoodputTxtBox.Enabled = false;
            this.totalGoodputTxtBox.Location = new System.Drawing.Point(444, 491);
            this.totalGoodputTxtBox.Name = "totalGoodputTxtBox";
            this.totalGoodputTxtBox.Size = new System.Drawing.Size(134, 26);
            this.totalGoodputTxtBox.TabIndex = 23;
            // 
            // udpGoodputLabel
            // 
            this.udpGoodputLabel.AutoSize = true;
            this.udpGoodputLabel.Location = new System.Drawing.Point(337, 285);
            this.udpGoodputLabel.Name = "udpGoodputLabel";
            this.udpGoodputLabel.Size = new System.Drawing.Size(110, 20);
            this.udpGoodputLabel.TabIndex = 24;
            this.udpGoodputLabel.Text = "UDP Goodput";
            // 
            // totalGoodputLabel
            // 
            this.totalGoodputLabel.AutoSize = true;
            this.totalGoodputLabel.Location = new System.Drawing.Point(48, 494);
            this.totalGoodputLabel.Name = "totalGoodputLabel";
            this.totalGoodputLabel.Size = new System.Drawing.Size(263, 20);
            this.totalGoodputLabel.TabIndex = 25;
            this.totalGoodputLabel.Text = "Total Goodput of Observed Network";
            // 
            // otherCountLabel
            // 
            this.otherCountLabel.AutoSize = true;
            this.otherCountLabel.Location = new System.Drawing.Point(53, 392);
            this.otherCountLabel.Name = "otherCountLabel";
            this.otherCountLabel.Size = new System.Drawing.Size(197, 20);
            this.otherCountLabel.TabIndex = 26;
            this.otherCountLabel.Text = "Other Traffic Packet Count";
            // 
            // otherGoodputTxtBox
            // 
            this.otherGoodputTxtBox.Enabled = false;
            this.otherGoodputTxtBox.Location = new System.Drawing.Point(444, 425);
            this.otherGoodputTxtBox.Name = "otherGoodputTxtBox";
            this.otherGoodputTxtBox.Size = new System.Drawing.Size(134, 26);
            this.otherGoodputTxtBox.TabIndex = 27;
            // 
            // otherGoodputLabel
            // 
            this.otherGoodputLabel.AutoSize = true;
            this.otherGoodputLabel.Location = new System.Drawing.Point(53, 428);
            this.otherGoodputLabel.Name = "otherGoodputLabel";
            this.otherGoodputLabel.Size = new System.Drawing.Size(164, 20);
            this.otherGoodputLabel.TabIndex = 28;
            this.otherGoodputLabel.Text = "Other Traffic Goodput";
            // 
            // arpRespTitle
            // 
            this.arpRespTitle.AutoSize = true;
            this.arpRespTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F);
            this.arpRespTitle.Location = new System.Drawing.Point(47, 55);
            this.arpRespTitle.Name = "arpRespTitle";
            this.arpRespTitle.Size = new System.Drawing.Size(170, 26);
            this.arpRespTitle.TabIndex = 29;
            this.arpRespTitle.Text = "ARP Responder";
            this.arpRespTitle.UseMnemonic = false;
            this.arpRespTitle.Click += new System.EventHandler(this.label12_Click);
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
            // icmpThroughputLabel
            // 
            this.icmpThroughputLabel.AutoSize = true;
            this.icmpThroughputLabel.Location = new System.Drawing.Point(9, 179);
            this.icmpThroughputLabel.Name = "icmpThroughputLabel";
            this.icmpThroughputLabel.Size = new System.Drawing.Size(134, 20);
            this.icmpThroughputLabel.TabIndex = 31;
            this.icmpThroughputLabel.Text = "ICMP Throughput";
            // 
            // icmpThroughputTxtBox
            // 
            this.icmpThroughputTxtBox.Enabled = false;
            this.icmpThroughputTxtBox.Location = new System.Drawing.Point(165, 176);
            this.icmpThroughputTxtBox.Name = "icmpThroughputTxtBox";
            this.icmpThroughputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.icmpThroughputTxtBox.TabIndex = 32;
            // 
            // tcpThroughputTxtBox
            // 
            this.tcpThroughputTxtBox.Enabled = false;
            this.tcpThroughputTxtBox.Location = new System.Drawing.Point(478, 176);
            this.tcpThroughputTxtBox.Name = "tcpThroughputTxtBox";
            this.tcpThroughputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.tcpThroughputTxtBox.TabIndex = 33;
            // 
            // tcpThroughputLabel
            // 
            this.tcpThroughputLabel.AutoSize = true;
            this.tcpThroughputLabel.Location = new System.Drawing.Point(322, 179);
            this.tcpThroughputLabel.Name = "tcpThroughputLabel";
            this.tcpThroughputLabel.Size = new System.Drawing.Size(125, 20);
            this.tcpThroughputLabel.TabIndex = 34;
            this.tcpThroughputLabel.Text = "TCP Throughput";
            // 
            // arpThroughputTxtBox
            // 
            this.arpThroughputTxtBox.Enabled = false;
            this.arpThroughputTxtBox.Location = new System.Drawing.Point(165, 312);
            this.arpThroughputTxtBox.Name = "arpThroughputTxtBox";
            this.arpThroughputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.arpThroughputTxtBox.TabIndex = 35;
            // 
            // udpThroughputTxtBox
            // 
            this.udpThroughputTxtBox.Enabled = false;
            this.udpThroughputTxtBox.Location = new System.Drawing.Point(478, 315);
            this.udpThroughputTxtBox.Name = "udpThroughputTxtBox";
            this.udpThroughputTxtBox.Size = new System.Drawing.Size(100, 26);
            this.udpThroughputTxtBox.TabIndex = 36;
            // 
            // arpThroughputLabel
            // 
            this.arpThroughputLabel.AutoSize = true;
            this.arpThroughputLabel.Location = new System.Drawing.Point(9, 315);
            this.arpThroughputLabel.Name = "arpThroughputLabel";
            this.arpThroughputLabel.Size = new System.Drawing.Size(128, 20);
            this.arpThroughputLabel.TabIndex = 37;
            this.arpThroughputLabel.Text = "ARP Throughput";
            // 
            // udpThroughputLabel
            // 
            this.udpThroughputLabel.AutoSize = true;
            this.udpThroughputLabel.Location = new System.Drawing.Point(318, 318);
            this.udpThroughputLabel.Name = "udpThroughputLabel";
            this.udpThroughputLabel.Size = new System.Drawing.Size(129, 20);
            this.udpThroughputLabel.TabIndex = 38;
            this.udpThroughputLabel.Text = "UDP Throughput";
            // 
            // gratArp
            // 
            this.gratArp.BackColor = System.Drawing.Color.Black;
            this.gratArp.ForeColor = System.Drawing.SystemColors.Window;
            this.gratArp.Location = new System.Drawing.Point(478, 55);
            this.gratArp.Name = "gratArp";
            this.gratArp.Size = new System.Drawing.Size(100, 25);
            this.gratArp.TabIndex = 39;
            // 
            // packetCaptureForm
            // 
            this.ClientSize = new System.Drawing.Size(624, 593);
            this.Controls.Add(this.gratArp);
            this.Controls.Add(this.udpThroughputLabel);
            this.Controls.Add(this.arpThroughputLabel);
            this.Controls.Add(this.udpThroughputTxtBox);
            this.Controls.Add(this.arpThroughputTxtBox);
            this.Controls.Add(this.tcpThroughputLabel);
            this.Controls.Add(this.tcpThroughputTxtBox);
            this.Controls.Add(this.icmpThroughputTxtBox);
            this.Controls.Add(this.icmpThroughputLabel);
            this.Controls.Add(this.callForHelpBtn);
            this.Controls.Add(this.arpRespTitle);
            this.Controls.Add(this.otherGoodputLabel);
            this.Controls.Add(this.otherGoodputTxtBox);
            this.Controls.Add(this.otherCountLabel);
            this.Controls.Add(this.totalGoodputLabel);
            this.Controls.Add(this.udpGoodputLabel);
            this.Controls.Add(this.totalGoodputTxtBox);
            this.Controls.Add(this.otherPacketCountTxtBox);
            this.Controls.Add(this.udpGoodputTxtBox);
            this.Controls.Add(this.udpCountLabel);
            this.Controls.Add(this.arpGoodputLabel);
            this.Controls.Add(this.arpCountLabel);
            this.Controls.Add(this.tcpGoodputLabel);
            this.Controls.Add(this.tcpCountLabel);
            this.Controls.Add(this.icmpGoodputLabel);
            this.Controls.Add(this.udpCountTxtBox);
            this.Controls.Add(this.arpGoodputTxtBox);
            this.Controls.Add(this.arpCountTxtBox);
            this.Controls.Add(this.tcpGoodputTxtBox);
            this.Controls.Add(this.tcpCountTxtBox);
            this.Controls.Add(this.icmpGoodputTxtBox);
            this.Controls.Add(this.icmpPktCntLabel);
            this.Controls.Add(this.icmpCntTxtBox);
            this.Controls.Add(this.totalPacketsLabel);
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
            
            txtNumPackets.Text = Convert.ToString(numPackets);
            icmpCntTxtBox.Text = Convert.ToString(icmpPacketsReceived);
            icmpGoodputTxtBox.Text = Convert.ToString((icmpThroughput - icmpOverhead));
            icmpThroughputTxtBox.Text = Convert.ToString(icmpThroughput);
            tcpCountTxtBox.Text = Convert.ToString(tcpPacketsReceived);
            tcpGoodputTxtBox.Text = Convert.ToString(tcpThroughput - tcpOverhead);
            tcpThroughputTxtBox.Text = Convert.ToString(tcpThroughput);
            arpCountTxtBox.Text = Convert.ToString(arpPacketsReceived);
            arpGoodputTxtBox.Text = Convert.ToString(arpThroughput - arpOverhead);
            arpThroughputTxtBox.Text = Convert.ToString(arpThroughput);
            udpCountTxtBox.Text = Convert.ToString(udpPacketsReceived);
            udpGoodputTxtBox.Text = Convert.ToString(udpThroughput - udpOverhead);
            udpThroughputTxtBox.Text = Convert.ToString(udpThroughput);
            otherPacketCountTxtBox.Text = Convert.ToString(otherPackets);
            otherGoodputTxtBox.Text = Convert.ToString(otherThroughput - otherOverhead);
            totalGoodputTxtBox.Text = Convert.ToString(totalGoodput);

            gratArp.Value = gratuitousArps;

            if (gratuitousArps < 100) {
                if(gratuitousArps > 50){
                    if(wallpaperChoice == -1){
                        wallpaperChoice = 0;
                        Wallpaper.Set(wallpaperChoice);
                    }
                }
            } else {
                if (gratuitousArps < 250)
                {
                    if (gratuitousArps > 150)
                    {
                        //Welcome to VaderVille
                        if (wallpaperChoice < 1)
                        {

                            negativeOffset = 100;
                            hundredMultiplier = 1.5f;

                            wallpaperChoice = 1;
                            Wallpaper.Set(wallpaperChoice);
                            soundPlayer.SoundLocation = "imperial-march.wav";
                            soundPlayer.Load();
                            soundPlayer.Play();
                        }
                    } else {

                        gratArp.Maximum = 250;
                        gratArp.Minimum = 100;
                        gratArp.ForeColor = Color.Black;
                    }
                }
                else
                {
                    if (gratuitousArps < 325)
                    {
                        //Welcome to Ry'leh
                        if (wallpaperChoice < 2)
                        {

                            wallpaperChoice = 2;
                            Wallpaper.Set(wallpaperChoice);
                            waveOutSetVolume(IntPtr.Zero, (uint)0);
                            soundPlayer.SoundLocation = "iaiacthulhu.wav";
                            soundPlayer.Load();
                            soundPlayer.PlayLooping();
                        }
                    } else {

                            gratArp.Maximum = 500;
                        gratArp.Minimum = 250;
                        negativeOffset = 250;
                        hundredMultiplier = 2.5f;
                    }
                }
            }

            var volumeToBe = (gratuitousArps - negativeOffset) / hundredMultiplier;
            

            waveOutSetVolume(IntPtr.Zero, (uint)volumeToBe);

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
            Wallpaper.Set(wallpaperChoice);
        }
    }
}

public sealed class volumeChange
{
    private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
    private const int APPCOMMAND_VOLUME_UP = 0xA0000;
    private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
    private const int WM_APPCOMMAND = 0x319;

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
        IntPtr wParam, IntPtr lParam);

    private void Mute()
    {
        SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
            (IntPtr)APPCOMMAND_VOLUME_MUTE);
    }

    private void VolDown()
    {
        SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
            (IntPtr)APPCOMMAND_VOLUME_DOWN);
    }

    private void VolUp()
    {
        SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
        (IntPtr)APPCOMMAND_VOLUME_UP);
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
    

    public static void Set(int wallpaperChoice)
    {

        string filename = "";

        switch(wallpaperChoice){
            case 0:
                filename = "stalin.jpg";
                break;
            case 1:
                filename = "vader.jpg";
                break;
            case 2:
                filename = "cthulhu.jpg";
                break;
        }

        System.Drawing.Image img = System.Drawing.Image.FromFile(filename);
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
