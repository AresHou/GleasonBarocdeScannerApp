using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.IO.Ports;


namespace BarcodeScanner
{
    
    public partial class Form1 : Form
    {
        SerialPort _serialPort;
        public static string barcode;
        public const int READ_BUFFER_SIZE = 32;

        private const string datav_login = "https://gleason.datav.bsquare.com/datav-login";
        private const string datav_gleason_inventory_with_barcode = "https://gleason.datav.bsquare.com/datav-gleason-inventory/#/gleasonInventory?barcode=";

        // For local test
        //private const string datav_login = "http://10.11.0.54:8080/datav-login";   
        //private const string datav_gleason_inventory_with_barcode = "http://10.11.0.54:8080/datav-console/#/home?targetUri=..%252Fdatav-gleason-inventory%252F%2523%252FgleasonInventory?barcode=";


        /// <summary> 
        /// Holds data received until we get a terminator. 
        /// </summary> 
        private string tString = string.Empty;
        /// <summary> 
        /// End of transmition byte EOT (ASCII 4). 
        /// </summary> 
        private byte _terminator = 0x4;
        /// <summary> 
        /// End of barcode '\r' (ASCII 4). 
        /// </summary>
        char _CarriageReturn = '\r';

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Console.WriteLine(webBrowser1.Url.ToString());
        }

        public Form1()
        {
            InitializeComponent();

            /* Read related parameters located at the BarcodeConfiguration.txt */
            string BarcodeConfigFileName = "BarcodeConfiguration.txt";        
            string BarcodeExePath = System.IO.Directory.GetCurrentDirectory();
            string BarcodeConfigPath = BarcodeExePath + "\\" + BarcodeConfigFileName;

            if (!File.Exists(BarcodeConfigPath)) {
                MessageBox.Show(BarcodeConfigPath + ". This path cannot find Bardcode configuration file ! ");
                System.Environment.Exit(1);
            }

            // Open the file to read from.
            string COMPort = File.ReadAllText(BarcodeConfigPath);
            Console.WriteLine(COMPort);

            /* Initialize seriial port infrastructure */
            _serialPort = new SerialPort(COMPort, 115200, Parity.None, 8, StopBits.One);
            //_serialPort = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            // set read time out to 0.5 seconds
            _serialPort.ReadTimeout = 500;
            
            _serialPort.Open();

            Thread.Sleep(1000);

            try
            {
                // Lanuch DataV login screen.
                webBrowser1.Navigate(new Uri(datav_login)); 
            } catch (System.UriFormatException)
            {
                MessageBox.Show("Fail to navigate to DataV login !! ");
            }            
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }       

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Initialize a buffer to hold the received data  
            byte[] buffer = new byte[READ_BUFFER_SIZE];

            // There is no accurate method for checking how many bytes are read 
            // unless you check the return from the Read method 
            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                MessageBox.Show("Read nothing !! ");
                return;
            }               

            // Assume the data we are received is ASCII data. 
            tString += Encoding.ASCII.GetString(buffer, 0, bytesRead);

            // Check if string contains the terminator  
            if (tString.IndexOf((char)_terminator) > -1)
            {
                // Cannot find 'EOT'
                // If tString does contain terminator we cannot assume that it is the last character received 
                string workingString = tString.Substring(0, tString.IndexOf((char)_terminator));
                
                // Remove the data up to the terminator from tString 
                tString = tString.Substring(tString.IndexOf((char)_terminator));
                
                // Do something with workingString 
                Console.WriteLine(workingString);
            }

            if (tString.IndexOf((char)_CarriageReturn, 0) == -1)
            {
                // Being not able to find character '\r', record it first!
                barcode = tString;
            }
            else
            {
                barcode += tString;      
               
                try
                {
                    // Send barcode to DataV-Gleason Inventory
                    webBrowser1.Refresh();
                    webBrowser1.Navigate(new Uri(datav_gleason_inventory_with_barcode + barcode));
                    webBrowser1.Navigated += webBrowser1_Navigated;
                }
                catch (System.UriFormatException)
                {
                    MessageBox.Show("Fail to vavigate to DataV-Gleason Inventory !! ");
                }

                MessageBox.Show("URL : " + datav_gleason_inventory_with_barcode + barcode);
            }

            // Clear string
            tString = string.Empty;

            // Clear buffer
            Array.Clear(buffer, 0, buffer.Length);

            // Thread.Sleep(100);

            //Discard data from the serial driver's receive buffer
            _serialPort.DiscardInBuffer();

            //_serialPort.Close();       
        }
    }
}
