using libStreamSDK;
using System;
using System.IO;
using System.Media;
using System.Threading;

namespace oddball_experiment
{


    public class SoundHCI
    {
        private SoundPlayer player1000 = null;
        private string wavFile1000 = "./1000hz_100ms.wav";
        private SoundPlayer player2000 = null;
        private string wavFile2000 = "./2000hz_100ms.wav";
        private StreamWriter wave_writer = null;
        private StreamWriter sound_log_writer = null;
        private const int EXPERIMENT_DURATION_SEC = 360;
        private const float INTERVAL_SEC = 0.8f;
        private const float LOW_SOUND_RATIO = 0.2f;
        private bool isExperimentFinished = false;

        public SoundHCI()
        {
            player1000 = new System.Media.SoundPlayer(wavFile1000);
            player2000 = new System.Media.SoundPlayer(wavFile2000);

            string dateStr = getDateStrForFilename();
            wave_writer = new StreamWriter("./raw_mind_wave" + dateStr +  ".csv", true);
            sound_log_writer = new StreamWriter("./sound_timing_log" + dateStr +  ".csv", true);

        }

        private void playHighSync()
        {
            sound_log_writer.WriteLine(getUnixtimeInMillisec().ToString() + ",2,0"); // log of sound start
            player2000.PlaySync();
            sound_log_writer.WriteLine(getUnixtimeInMillisec().ToString() + ",2,1"); // log of sound end
        }

        private void playLowSync()
        {
            sound_log_writer.WriteLine(getUnixtimeInMillisec().ToString() + ",1,0"); // log of sound start
            player1000.PlaySync();
            sound_log_writer.WriteLine(getUnixtimeInMillisec().ToString() + ",1,1"); // log of sound end
        }

        private void soundLoop()
        {
            Random rnd = new Random(1000);
            long start_time = getUnixtimeInMillisec();
            while(getUnixtimeInMillisec() - start_time < EXPERIMENT_DURATION_SEC * 1000)
            {
                int rnd_val = rnd.Next(10);
                if(rnd_val <= 1)
                {
                    playLowSync();
                }
                else
                {
                    playHighSync();
                }
                sound_log_writer.Flush();
                Thread.Sleep((int)(INTERVAL_SEC * 1000));
            }
            sound_log_writer.Close();
            isExperimentFinished = true;
        }

        private long getUnixtimeInMillisec()
        {
            DateTimeOffset baseDt = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            long unixtime = (long) ((DateTimeOffset.Now - baseDt).Ticks / 10000.0); // convert to milli secods integer
            // return String.Format("{0:##########.###}", unixtime);
            return unixtime;
        }

        private String getDateStrForFilename()
        {
            DateTime dt = DateTime.Now;
            return dt.ToString("yyyyMMdd_HHmmss");
        }

        private String getDateStrForLog()
        {
            DateTime dt = DateTime.Now;
            return dt.ToString("yyyy/MM/dd HH:mm:ss:fff");
        }

        public void startExperiment()
        {
            NativeThinkgear thinkgear = new NativeThinkgear();

            /* Print driver version number */
            Console.WriteLine("Version: " + NativeThinkgear.TG_GetVersion());

            /* Get a connection ID handle to ThinkGear */
            int connectionID = NativeThinkgear.TG_GetNewConnectionId();
            Console.WriteLine("Connection ID: " + connectionID);

            if (connectionID < 0)
            {
                Console.WriteLine("ERROR: TG_GetNewConnectionId() returned: " + connectionID);
                return;
            }

            int errCode = 0;


            /* Attempt to connect the connection ID handle to serial port "COM5" */
            //string comPortName = "\\\\.\\COM5";
            string comPortName = "\\\\.\\COM6";

            errCode = NativeThinkgear.TG_Connect(connectionID,
                          comPortName,
                          NativeThinkgear.Baudrate.TG_BAUD_57600,
                          NativeThinkgear.SerialDataFormat.TG_STREAM_PACKETS);
            if (errCode < 0)
            {
                Console.WriteLine("ERROR: TG_Connect() returned: " + errCode);
                return;
            }

            ///* Read 10 ThinkGear Packets from the connection, 1 Packet at a time */
            int packetsRead = 0;
            Console.WriteLine("auto read test begin:");

            errCode = NativeThinkgear.TG_EnableAutoRead(connectionID, 1);
            if (errCode == 0)
            {
                packetsRead = 0;
                Thread sound_th = new Thread(new ThreadStart(soundLoop));
                sound_th.Start();
                while (isExperimentFinished == false)
                {
                    /* If raw value has been updated ... */
                    if (NativeThinkgear.TG_GetValueStatus(connectionID, NativeThinkgear.DataType.TG_DATA_RAW) != 0)
                    {

                    float raw_val = NativeThinkgear.TG_GetValue(connectionID, NativeThinkgear.DataType.TG_DATA_RAW);
                    wave_writer.WriteLine(getDateStrForLog() + "," + getUnixtimeInMillisec().ToString() +  "," + raw_val.ToString());
                    packetsRead++;
                    }
                }
                errCode = NativeThinkgear.TG_EnableAutoRead(connectionID, 0); //stop
                Console.WriteLine("auto read test stoped: " + errCode);
            }
            else
            {
                Console.WriteLine("auto read test failed: " + errCode);
            }

            NativeThinkgear.TG_Disconnect(connectionID); // disconnect test

            /* Clean up */
            NativeThinkgear.TG_FreeConnection(connectionID);

            /* End program */
            Console.WriteLine("please type Enter key to finish this program.");
            Console.ReadLine();
        }
    }
}
