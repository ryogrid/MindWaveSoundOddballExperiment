using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AnalyzeExperimentData
{
    class DataLoadAndAggregate
    {
        private const int AGGREGATE_WAVES = 30;

        private StreamReader wave_reader_cnt_low;
        private StreamReader sound_timing_reader_cnt_low;
        private StreamReader wave_reader_cnt_high;
        private StreamReader sound_timing_reader_cnt_high;
        private StreamWriter cnt_low_low_sum_writer = null;
        private StreamWriter cnt_low_high_sum_writer = null;
        private StreamWriter cnt_high_low_sum_writer = null;
        private StreamWriter cnt_high_high_sum_writer = null;
        private double[] cnt_low_low_sum = new double[2000];
        private double[] cnt_low_high_sum = new double[2000];
        private double[] cnt_high_low_sum = new double[2000];
        private double[] cnt_high_high_sum = new double[2000];
        private double[] cnt_all_sum = new double[2000];

        public DataLoadAndAggregate()
        {
            ReadFilesOpen();

            cnt_low_low_sum_writer = new StreamWriter("../../experiment_data/experiment_result1025_100ms/count_low/cnt_low_low_average20191025_095533.csv", false);
            cnt_low_high_sum_writer = new StreamWriter("../../experiment_data/experiment_result1025_100ms/count_low/cnt_low_high_average20191025_095533.csv", false);
            cnt_high_low_sum_writer = new StreamWriter("../../experiment_data/experiment_result1025_100ms/count_high/cnt_high_low_average20191025_095056.csv", false);
            cnt_high_high_sum_writer = new StreamWriter("../../experiment_data/experiment_result1025_100ms/count_high/cnt_high_high_average20191025_095056.csv", false);
        }


        //既に一度開いたものであればCloseして再度Open
        private void ReadFilesOpen()
        {
            if (wave_reader_cnt_low != null)
            {
                wave_reader_cnt_low.Close();
            }
            wave_reader_cnt_low = new StreamReader("../../experiment_data/experiment_result1025_100ms/count_low/raw_mind_wave20191025_095533.csv", Encoding.GetEncoding("Shift_JIS"));

            if (sound_timing_reader_cnt_low != null)
            {
                sound_timing_reader_cnt_low.Close();
            }
            sound_timing_reader_cnt_low = new StreamReader("../../experiment_data/experiment_result1025_100ms/count_low/sound_timing_log20191025_095533.csv", Encoding.GetEncoding("Shift_JIS"));

            if (wave_reader_cnt_high != null)
            {
                wave_reader_cnt_high.Close();
            }
            wave_reader_cnt_high = new StreamReader("../../experiment_data/experiment_result1025_100ms/count_high/raw_mind_wave20191025_095056.csv", Encoding.GetEncoding("Shift_JIS"));

            if (sound_timing_reader_cnt_high != null)
            {
                sound_timing_reader_cnt_high.Close();
            }
            sound_timing_reader_cnt_high = new StreamReader("../../experiment_data/experiment_result1025_100ms/count_high/sound_timing_log20191025_095056.csv", Encoding.GetEncoding("Shift_JIS"));

        }

        private long[] getSoundStartTime(StreamReader soundTimingSR)
        {
            while (true)
            {
                var line = soundTimingSR.ReadLine();
                if(line == null)
                {
                    return null;
                }
                else
                {
                    string[] splited = line.Split(",".ToCharArray());
                    long unixtime_milli = long.Parse(splited[0]);
                    long sound_type = long.Parse(splited[1]);
                    int start_or_end = int.Parse(splited[2]);
                    if(start_or_end == 0)
                    {
                        return new long[2] { unixtime_milli, sound_type};
                    }
                }
            }
        }

        private void generateAggregatedData(StreamReader sndTimingSR, StreamReader mindWaveSR, StreamWriter aggregatedSW, double[] aggregateDestArr, bool isLow)
        {
            long[] timingInfo1 = getSoundStartTime(sndTimingSR);
            long cur_start = timingInfo1[0];
            bool is_cur_start_low = timingInfo1[1] == 1 ? true : false;
            long[] timingInfo2 = getSoundStartTime(sndTimingSR);
            long next_start = timingInfo2[0];
            bool is_next_start_low = timingInfo2[1] == 1 ? true : false;


            if (is_cur_start_low != isLow)
            {
                while (true)
                {
                    cur_start = next_start;
                    is_cur_start_low = is_next_start_low;
                    long[] time_and_sound_type = getSoundStartTime(sndTimingSR);
                    if (time_and_sound_type == null)
                    {
                        break; //サウンドファイルを最後まで読んだ
                    }
                    next_start = time_and_sound_type[0];
                    is_next_start_low = time_and_sound_type[1] == 1 ? true : false;

                    // 真理値が一致するかのチェックはtrueで一致する場合も, falseで一致する場合もある
                    if (is_cur_start_low == isLow)
                    {
                        break; //現在集計対象のサウンドの回に移動できた
                    }
                }
            }

            int agg_arr_signal_idx = 0;
            int added_waves = 0;
            string line = null;
            while((line = mindWaveSR.ReadLine()) != null){
                string[] splited = line.Split(",".ToCharArray());

                //要素が全て出力されていない行があったりするようなので、その場合は飛ばす
                if (splited.Length < 3)
                {
                    continue;
                }

                long signal_timing = long.Parse(splited[1]);


                if (signal_timing >= cur_start && signal_timing <= next_start)
                {
                    double signal_val = Double.Parse(splited[2]);
                    aggregateDestArr[agg_arr_signal_idx] += signal_val;
                    agg_arr_signal_idx++;
                }
                else if (signal_timing < cur_start) //対象とするの区間のサンプルに達していない場合
                {
                    continue;
                }
                else //次のサウンドの回に移る
                {
                    while (true)
                    {
                        cur_start = next_start;
                        is_cur_start_low = is_next_start_low;
                        long[] time_and_sound_type = getSoundStartTime(sndTimingSR);
                        if (time_and_sound_type == null)
                        {
                            break; //サウンドファイルを最後まで読んだ
                        }
                        next_start = time_and_sound_type[0];
                        is_next_start_low = time_and_sound_type[1] == 1 ? true : false;

                        // 真理値が一致するかのチェックはtrueで一致する場合も, falseで一致する場合もある
                        if (is_cur_start_low == isLow)
                        {
                            Console.WriteLine("move nett target sound: type=" + is_cur_start_low.ToString() + " added_waves=" + added_waves.ToString());
                            added_waves++;
                            if(added_waves == AGGREGATE_WAVES)
                            {
                                goto finish;
                            }
                            agg_arr_signal_idx = 0;
                            break; //現在集計対象のサウンドの回に移動できた
                        }
                    }
                }
            }
finish:
            //加算した値を、加算した回数だけ割ることで平均値にしてファイルに書きだす
            int arr_length = aggregateDestArr.Length;
            for(int i = 0; i < arr_length; i++)
            {
                aggregateDestArr[i] /= ((double)added_waves);
                aggregatedSW.WriteLine(aggregateDestArr[i].ToString("####.###"));
            }
            aggregatedSW.Close();
        }

        public void startExecution()
        {
            // count low low signal
            Console.WriteLine("start analyze low low");
            generateAggregatedData(sound_timing_reader_cnt_low, wave_reader_cnt_low, cnt_low_low_sum_writer, cnt_low_low_sum, true);
            // count low high signal
            ReadFilesOpen();
            Console.WriteLine("start analyze low high");
            generateAggregatedData(sound_timing_reader_cnt_low, wave_reader_cnt_low, cnt_low_high_sum_writer, cnt_low_high_sum, false);

            // count high low signal
            Console.WriteLine("start analyze high low");
            generateAggregatedData(sound_timing_reader_cnt_high, wave_reader_cnt_high, cnt_high_low_sum_writer, cnt_high_low_sum, true);
            // count high high sinal
            ReadFilesOpen();
            Console.WriteLine("start analyze high high");
            generateAggregatedData(sound_timing_reader_cnt_high, wave_reader_cnt_high, cnt_high_high_sum_writer, cnt_high_high_sum, false);

            Console.WriteLine("please type Enter key to finish this program.");
            Console.ReadLine();
        }
    }
}
