//using System;
using System.Threading;
using System.IO.Ports;
using System.Text;
using System.IO;
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.FEZ;
//using GHIElectronics.NETMF.Hardware;

using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;


namespace FEZ_Client
{
    public class Program
    {
        static bool newstring = false;
        static string datastring;
    
        /// <summary>
        /// Interrupt service routine for Button push my button is located on Digital IO pin 16 and is set up for Falling edge capture
        /// </summary>
        /// <param name="port"></param>
        /// <param name="state"></param>
        /// <param name="time"></param>
        static void IntButton_ONInterrupt(uint port, uint state, DateTime time)  
        {
            while (newstring);  //wait for network stream to be empty and load buffer with new string
            
            datastring = "OUCH!\n";
                newstring = true;

        }
        /// <summary>
        /// This code will create a TCP socket between this DHCP client and a server node on a specific IP and Port.
        /// ServerIP can be modified to point at the correct Server IP
        /// the main also spins off a serialport thread to monitor the serial port for new GPS Data.
        /// </summary>

        public static void Main()
        {
               SoftwareI2C tempS = new SoftwareI2C((Cpu.Pin)FEZ_Pin.Digital.IO66, (Cpu.Pin)FEZ_Pin.Digital.IO65,400);

                byte[] inBuffer = new byte[2];
                byte[] A = new byte[] { 0x03 }; // A command

                tempS.Transmit(true, false, 0x90); //address
                tempS.Transmit(false, false, 0x00); //register num
                tempS.Transmit(true, false, 0x91); //send read address

                inBuffer[0] = tempS.Receive(true, false);
                inBuffer[1] = tempS.Receive(true, true);

            byte[] mac = { 0x00, 0x26, 0x1c, 0x7b, 0x29, 0xE8 };  //The Network board does not come pre installed with a MAC address 
            //Please use(http://www.macvendorlookup.com/) to create a Mac address for you device

            WIZnet_W5100.Enable(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di7, true);  //initializes the Panda II hardware to to connect to the FEZ Connect
            //NetworkInterface.EnableStaticIP(ip, subnet, gateway,mac); //This can be used to intialize your client connection if you want to hard code in the network configuration instead of using DHCP
            Dhcp.EnableDhcp(mac, "FEZ");  //Intialize FEZ connect using the mac address "FEZ" is the name of the host change this for multiple clients
            Socket mysocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //create network socket 
            byte[] ServerIp = { 10, 10, 4, 24 }; //change this to your Server IP
            mysocket.SendTimeout = 3000;
            mysocket.Connect(new IPEndPoint(new IPAddress(ServerIp),9090)); //Connect to server using socket
          
            OutputPort LED = new OutputPort((Cpu.Pin)69, true);
            LED.Write(!LED.Read());
            string message = "hello Mom\n";
            mysocket.Send(Encoding.UTF8.GetBytes(message));
            InterruptPort IntButton = new InterruptPort((Cpu.Pin)FEZ_Pin.Interrupt.IO4,true,Port.ResistorMode.PullUp,Port.InterruptMode.InterruptEdgeLow); //create button interrupt
            IntButton.OnInterrupt+= new NativeEventHandler(IntButton_ONInterrupt);
            
            int value;
            
            while (true)
            {
                if (newstring)
                {                  
                      Debug.Print(datastring);
                      mysocket.Send(Encoding.UTF8.GetBytes(datastring));        //write string to network socket
                      newstring = false;
                      LED.Write(!LED.Read());
                }
                tempS.Transmit(true, false, 0x90); //send I2C Device write address for dummy write
                tempS.Transmit(false, false, 0x00); //Send Register read address
                tempS.Transmit(true, false, 0x91); //send I2C Device Read address

                inBuffer[0] = tempS.Receive(true, false); //Read MSB (bits 11-4)
                inBuffer[1] = tempS.Receive(true, true);  //Read LSB (bits 0-3 left justified)

                value = (int)inBuffer[0]*256 + inBuffer[0]; //combine MSB and LSB
                value = value>>4;                           //Shift value to right by 4 bits to right justify "value" holds the temperature in deg. C with .0625 Deg. Resolution             
                value = (int) ((float)value * .0625);       //The "value" variable now holds the temp in degrees C.
                value = (9 * value) / 5 + 32;               //Convert to deg. f

                Debug.Print(value.ToString());              //Print "value" as a string to the debug port
                if (datastring != (value.ToString() + "\n"))
                {
                    datastring = value.ToString() + "\n";
                    newstring = true;
                }
                Thread.Sleep(500);
            }
        }
    }
}
