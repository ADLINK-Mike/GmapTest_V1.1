using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using DDS;
using DDS.OpenSplice;
using HelloWorldData;
using DM;
using DDSAPIHelper;
using System.IO;




namespace GmapTest
{
    // Define global variable
    public static class GlobalData
    {
        public static bool ischanged = false;
        public static double upleft_lat;
        public static double upleft_lng;
        public static double btmright_lat;
        public static double btmright_lng;
        // new get data from DDS instead of text file
        public static String DDSPartitionName = "";
        public static bool DDS_Data_Received = false;
        public static String DDS_Lines = "";
        public static char theLF= '\n';
        public static bool WorkerThreadIsRunning = false;

    }
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // NOW force input for DDS Partition name
            //



            //Thread thr_DDS_Worker = new Thread(new ThreadStart(Program.OpenSpliceWorker));
            //Thread thr_DDS_Worker = new Thread(() => OpenSpliceWorker(Form1.ActiveForm));
            //thr_DDS_Worker.Start();
            //Thread thr_DDS_Worker = new Thread(new ThreadStart(Program.OpenSpliceWorker));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

        }


        // Create the DDS worker thread here.
        public static void OpenSpliceWorker(Form mainForm)
        {// Initialize the DDS Code first
            bool done = false;
            DDSEntityManager mgr = new DDSEntityManager("HelloWorld");
//            String partitionName = "";
//            if (GlobalData.DDSPartitionName.Length > 0)
//            {// Use the glbal name
//                partitionName = GlobalData.DDSPartitionName;
//            } else
//            {
//                partitionName = "ADLINK";
//            }
            //
            // create default Hello World Topic
            //
            // create Domain Participant
            // set QOS for entity manager...
            mgr.setDurabilityKind("transient");
            mgr.createParticipant(GlobalData.DDSPartitionName);
            mgr.setAutoDispose(false);
            // create Type
            MsgTypeSupport msgTS = new MsgTypeSupport();
            mgr.registerType(msgTS);
            // create Topic
            mgr.createTopic("HelloWorldData_Msg");
            // create Publisher
            mgr.createPublisher();
            // create DataWriter
            mgr.createWriter();
            //
            // NOW CREATE NEW DDS DATA RECEIVER
            //
            // NOW Create another HelloWorld.Msg with a different topic name.
            //
            DDSEntityManager ADSBDatamgr = new DDSEntityManager("ADSBLinesData");
            // create Domain Participant
            ADSBDatamgr.createParticipant(GlobalData.DDSPartitionName);
            // create Type DM::Aircraft_Config
            Aircraft_ConfigTypeSupport AircraftConfigMsg = new DM.Aircraft_ConfigTypeSupport();
            //MsgTypeSupport msgTS2 = new MsgTypeSupport();
            ADSBDatamgr.registerType(AircraftConfigMsg);
            //ADSBDatamgr.registerType(msgTS2);
            // create Topic
            ADSBDatamgr.createTopic("ADSBLinesData");
            // create Subscriber
            ADSBDatamgr.createSubscriber();
            //            ADSBDatamgr.createPublisher();
            // create DataReader
            ADSBDatamgr.createReader(false);
            IDataReader ADSBLinesReader = ADSBDatamgr.getReader();
            Aircraft_ConfigDataReader ADSBLinesDataReader = ADSBLinesReader as Aircraft_ConfigDataReader;
            Aircraft_Config ADSBLinesmsg = null;
            Aircraft_Config[] ADSBLinesmsgSeq = null;
            DDS.SampleInfo[] ADSBLinesinfoSeq = null;

            // Publish Events
            IDataWriter dwriter = mgr.getWriter();
            MsgDataWriter helloWorldWriter = dwriter as MsgDataWriter;

            Msg msgInstance = new Msg();
            msgInstance.userID = 1;
            msgInstance.message = "Update Locations"; // "Hello World";

            InstanceHandle handle = helloWorldWriter.RegisterInstance(msgInstance);
            ErrorHandler.checkHandle(handle, "MsgDataWriter.RegisterInstance");

            Console.WriteLine("=== [Publisher] writing a message containing :");
            Console.WriteLine("    userID  : {0}", msgInstance.userID);
            Console.WriteLine("    Message : \" {0} \"", msgInstance.message);
            ReturnCode status = helloWorldWriter.Write(msgInstance, handle);
            ErrorHandler.checkStatus(status, "MsgDataWriter.Write");

            // main write loop here...
            //try
            //{
            //    Thread.Sleep(2);
            //}
            //catch (ArgumentOutOfRangeException ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //    Console.WriteLine(ex.StackTrace);
            //}
            while (!done)
            {// sleep and write samples from the main screen.
                Thread.Sleep(500);

                if (GlobalData.ischanged == true)
                {// main thread has written data values, so update data
                    Console.WriteLine("GLOBAL DATA HAS CHANGED!!!");
                    msgInstance.lattitued_ul = GlobalData.upleft_lat;
                    msgInstance.longitude_ul = GlobalData.upleft_lng;
                    msgInstance.lattitude_lr = GlobalData.btmright_lat;
                    msgInstance.longitude_lr = GlobalData.btmright_lng;
                    Console.WriteLine("Upper left Lat" + msgInstance.lattitued_ul.ToString());
                    Console.WriteLine("Upper left Lng" + msgInstance.longitude_ul.ToString());
                    Console.WriteLine("Lower Right Lat" + msgInstance.lattitude_lr.ToString());
                    Console.WriteLine("Lower Right Lng" + msgInstance.longitude_lr.ToString());
                    ReturnCode dds_status = helloWorldWriter.Write(msgInstance, handle);
                    Console.WriteLine("HELLO WORLD DDS WRITE ERROR = " + dds_status.ToString());
                    ErrorHandler.checkStatus(dds_status, "MsgDataWriter.Write");

                    GlobalData.ischanged = false;
                } // globaldata.ischange = true
                // now read DDS Data lines topic.
                //
                // get new DDS Values here...
                status = ADSBLinesDataReader.Take(ref ADSBLinesmsgSeq, ref ADSBLinesinfoSeq, Length.Unlimited, SampleStateKind.Any, ViewStateKind.Any, InstanceStateKind.Any);
                ErrorHandler.checkStatus(status, "DataReader.Take");
                if (ADSBLinesmsgSeq.Length > 0)
                {
                    for (int x = 0; x < ADSBLinesmsgSeq.Length; x++)
                    {
                        if (ADSBLinesinfoSeq[x].ValidData)
                        {// read the data!
                            ADSBLinesmsg = ADSBLinesmsgSeq[x];
                            GlobalData.DDS_Lines = ADSBLinesmsg.tailNumber;
                            GlobalData.DDS_Data_Received = true;
/*                            Console.WriteLine("=== [Subscriber] message received : " + x.ToString());
                            Console.WriteLine("    lattitued_ul  : " + msgSeq[x].lattitued_ul.ToString());
                            Console.WriteLine("    longitude_ul  : " + msgSeq[x].longitude_ul.ToString());
                            Console.WriteLine("    lattitued_lr  : " + msgSeq[x].lattitude_lr.ToString());
                            Console.WriteLine("    longitude_lr  : " + msgSeq[x].longitude_lr.ToString());
                            Console.WriteLine("    Message : \"" + msgSeq[x].message + "\"");
*/
                            //                        status = HelloWorldDataReader.ReturnLoan(ref msgSeq, ref infoSeq);
                            //ErrorHandler.checkStatus(status, "DataReader.ReturnLoan");
                            //                            System.Threading.Thread.Sleep(2000);
/*                            lamin = msgSeq[x].lattitude_lr;  //32.3382F;
                            lomin = msgSeq[x].longitude_ul;  //- 121.8863F;
                            lamax = msgSeq[x].lattitued_ul;  //35.3382F;
                            lomax = msgSeq[x].longitude_lr;  // - 116.8863F;  */                        }
                    }
                }

                if (GlobalData.WorkerThreadIsRunning == false )
                {// signal thread to shut down...
                    done = true;
                }
            }

            // end of main write loop

            // Clean up
            status = helloWorldWriter.UnregisterInstance(msgInstance, handle);

            mgr.getPublisher().DeleteDataWriter(helloWorldWriter);
            mgr.deletePublisher();
            mgr.deleteTopic();
            mgr.deleteParticipant();

            return;
        }
    }
}
