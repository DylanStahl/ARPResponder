using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyPacketCapturer
{
    public partial class sendPacketForm : Form
    {
        public static int instantiations = 0;
        public static bool captureHasBeenSaved = false;

        public sendPacketForm()
        {
            InitializeComponent();
            ++instantiations;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|All Files| *.*";
            openFileDialog1.Title = "Open Captured Packets";
            openFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (openFileDialog1.FileName != "")
            {
                txtCaptureData.Text = "";
                txtCaptureData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
            else
            {
                DateTime currentDate = DateTime.Now;

                System.IO.File.WriteAllText("Capture_" + currentDate.ToShortDateString() + ".txt", txtCaptureData.Text);
                txtCaptureData.Text = "";
                txtCaptureData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|All Files| *.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCaptureData.Text);
                captureHasBeenSaved = true;
            }
        }

        private void sendPacketForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            --instantiations;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string stringBytes = "";

            //Get the hex values from the file
            foreach (string s in txtCaptureData.Lines) {
                string[] noComments = s.Split('#');
                stringBytes += noComments[0] + Environment.NewLine;
            }

            //Extract those hex values into string array.
            string[] sBytes = stringBytes.Split(new string[] { "\n", " ", "\r","\t", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            //Change the strings into Bytes
            byte[] packet = new byte[sBytes.Length];
            int i = 0;
            foreach (string s in sBytes) {
                packet[i] = Convert.ToByte(s, 16);
                ++i;
            }
             
            try
            {
                packetCaptureForm.device.SendPacket(packet);

            }
            catch (Exception ex) {
                //Ignore it.
            }

            //end btnSend
        }
    }
}
