using System;
using System.Windows.Controls;
using System.Windows.Input;
using LyricEditor.Lyric;

namespace LyricEditor.UserControls
{
    /// <summary>
    /// LrcLinesView.xaml 的交互逻辑
    /// </summary>
    public partial class LrcLineView : UserControl
    {
        public LrcLineView()
        {
            InitializeComponent();
            //button_get_trade_record.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(this.LrcLinePanel_MouseRightButtonDown), true);


            LrcLinePanel.Items.Clear();
            CurrentTimeText.Clear();
            CurrentLrcText.Clear();

            Manager = new LrcManager();
        }
        public LrcManager Manager { get; set; }
        public MainWindow MyMainWindow;
        bool SelectionChanged已播放;
        bool 右键触发的;

        //public double  jumoTime = 0; // 根据下一行的起始时间得出的句末时间
        public TimeSpan jumoTime { get; set; } = new TimeSpan(0, 0, 0, 0, 0); // 根据下一行的起始时间得出的句末时间

        public TimeSpan TimeOffset { get; set; } = new TimeSpan(0, 0, 0, 0, -150);
        public bool ApproxTime { get; set; } = false;

        private TimeSpan GetApproxTime(TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds, time.Milliseconds / 10 * 10);
        }

        public bool HasSelection { get => SelectedIndex != -1; }
        public int SelectedIndex
        {
            get => LrcLinePanel.SelectedIndex;
            set => LrcLinePanel.SelectedIndex = value;
        }
        public LrcLine SelectedItem
        {
            get { return LrcLinePanel.SelectedItem as LrcLine; }
            set { LrcLinePanel.SelectedItem = value; }
        }
        public bool ReachEnd
        {
            get => SelectedIndex == LrcLinePanel.Items.Count - 1;
        }
        /// <summary>
        /// 修改了单行的信息后，更新歌词列表的显示
        /// </summary>
        public void RefreshLrcPanel()
        {
            LrcLinePanel.Items.Refresh();
        }
        /// <summary>
        /// 同步 Manager 与歌词列表
        /// </summary>
        public void UpdateLrcPanel()
        {
            Manager.UpdateLrcList(LrcLinePanel);
        }
        /// <summary>
        /// 根据选择的行数更改下方文本框的内容
        /// </summary>
        public void UpdateBottomTextBoxes()
        {
            // 如果只选中了一项
            if (LrcLinePanel.SelectedItems.Count == 1)
            {
                LrcLine line = LrcLinePanel.SelectedItem as LrcLine;
                if (!(line.LrcTime is null))
                    CurrentTimeText.Text = line.LrcTimeText;
                else
                    CurrentTimeText.Clear();
                CurrentLrcText.Text = line.LrcText;
            }
        }


    

        /// <summary>
        /// 更改时间框的文本，更新主列表
        /// </summary>
        private void CurrentTime_Changed(object sender, TextChangedEventArgs e)
        {
            if (!HasSelection) return;

            int index = SelectedIndex;
            if (LrcHelper.TryParseTimeSpan(CurrentTimeText.Text, out TimeSpan time))
            {
                Manager.LrcList[index].LrcTime = time;
                ((LrcLine)LrcLinePanel.Items[index]).LrcTime = time;
                RefreshLrcPanel();
            }
            else if (string.IsNullOrWhiteSpace(CurrentTimeText.Text))
            {
                Manager.LrcList[index].LrcTime = null;
                ((LrcLine)LrcLinePanel.Items[index]).LrcTime = null;
                RefreshLrcPanel();
            }
        }
        /// <summary>
        /// 更改歌词框的文本，更新主列表
        /// </summary>
        private void CurrentLrc_Changed(object sender, TextChangedEventArgs e)
        {
            if (!HasSelection) return;

            int index = SelectedIndex;
            Manager.LrcList[index].LrcText = CurrentLrcText.Text;
            ((LrcLine)LrcLinePanel.Items[index]).LrcText = CurrentLrcText.Text;
            RefreshLrcPanel();
        }
        /// <summary>
        /// 在时间框中使用滚轮
        /// </summary>
        private void CurrentTimeText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 如果没有选中任意一行
            if (!HasSelection) return;
            // 如果当前时间栏为空
            if (string.IsNullOrWhiteSpace(CurrentTimeText.Text)) return;
            // 如果选中行的时间不存在（信息行）
            // 下面这行理论上是不需要的，因为如果是信息行，那么时间栏应该就是空的
            //if (SelectedItem.LrcTime is null) return;

            int index = SelectedIndex;
            var currentTime = Manager.LrcList[index].LrcTime.Value.TotalSeconds;
            if (e.Delta > 0)
            {
                AdjustCurrentLineTime(new TimeSpan(0, 0, 0, 0, 50));
            }
            else
            {
                AdjustCurrentLineTime(new TimeSpan(0, 0, 0, 0, -50));
            }
        }


        ///<summary>
        ///根据下一行的时间，设置停止时间
        ///</summary>
        ///
        public void SetJumoTime( TimeSpan CurrentTime)
        {

            if(MyMainWindow.MediaPlayer.Position.TotalMilliseconds<100)
            {//双击歌词播放时，position值更新慢
                int index = SelectedIndex;
                LrcLine nextLine = null;
                if (index < 0) return;

                if (index < LrcLinePanel.Items.Count - 1) //读取下一行
                {
                    nextLine = LrcLinePanel.Items[index + 1] as LrcLine;
                    jumoTime = nextLine.LrcTime.Value;
                }
                else
                {
                    //jumoTime = new TimeSpan();
                    jumoTime = MyMainWindow.MediaPlayer.NaturalDuration.TimeSpan;
                }

            }
            else
            {
                jumoTime = MyMainWindow.MediaPlayer.NaturalDuration.TimeSpan;
                foreach (LrcLine line in LrcLinePanel.Items)
                {//根据当前选中行设置句末时间
                    if (line.LrcTime.Value.TotalMilliseconds > CurrentTime.TotalMilliseconds)
                    {
                        //MyMainWindow.TmpStop();
                        jumoTime = line.LrcTime.Value;
                        break;

                    }
                }
            }



        }


        /// <summary>
        /// 歌词窗口的选择项发生改变
        /// </summary>
        private void LrcLinePanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!HasSelection) return;
            
            //UpdateBottomTextBoxes();
            if (右键触发的 == true)
            {
                Console.WriteLine("SelectionChanged,右键触发的={0}", 右键触发的);
                右键触发的 = false;
                return;
            }

            play播放本行();
            SelectionChanged已播放 = true;
        }

        /// <summary>
        /// 双击主列表，跳转播放时间
        /// </summary>
        private void LrcLinePanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            return;

        }

        private void LrcLinePanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("LrcLinePanel_MouseLeftButtonUp");
            if (SelectionChanged已播放 == true)
            {
                Console.WriteLine("SelectionChanged已播放");
                SelectionChanged已播放 = false;
                return;
            }
            play播放本行();
        }

        private void LrcLinePanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("LrcLinePanel_MouseRightButtonDown");
            右键触发的 = true;

        }

        private void play播放本行()
        {
            Console.WriteLine("play播放本行");
            if (!HasSelection) return;

            LrcLine line = LrcLinePanel.SelectedItem as LrcLine;
            if (!line.LrcTime.HasValue) return;
            MyMainWindow.LinePlay_DoubleClick(line);// 增加功能，双击行自动开始播放

            MyMainWindow.MediaPlayer.Position = line.LrcTime.Value;
        }

        /// <summary>
        /// 在主列表上使用按键
        /// </summary>
        private void LrcLinePanel_KeyUp(object sender, KeyEventArgs e)
        {
            //Console.WriteLine("upupupupupupupupupupupu");
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteLine();
                    break;
                case Key.Enter:
                    play播放本行();
                    break;
            }
        }

        private void AdjustCurrentLineTime(TimeSpan delta)
        {
            int index = SelectedIndex;

            var currentTime = Manager.LrcList[index].LrcTime.Value.Add(delta);
            if (currentTime < TimeSpan.Zero) currentTime = TimeSpan.Zero;

            Manager.LrcList[index].LrcTime = currentTime;
            ((LrcLine)LrcLinePanel.Items[index]).LrcTime = currentTime;

            UpdateBottomTextBoxes();
        }

        public void SetCurrentLineTime(TimeSpan time)
        {
            if (!HasSelection) return;
            int index = SelectedIndex;

            // 判断是否为歌曲信息行
            if (!Manager.LrcList[index].LrcTime.HasValue) return;

            time += TimeOffset;
            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            // 更新选中行的时间
            Manager.LrcList[index].LrcTime = time;
            ((LrcLine)LrcLinePanel.Items[index]).LrcTime = time;

            // 根据是否到达最后一行来设定下一个选中行
            if (!ReachEnd)
            {
                SelectedIndex++;
            }
            else
            {
                SelectedIndex = -1;
            }

            RefreshLrcPanel();
            LrcLinePanel.ScrollIntoView(LrcLinePanel.SelectedItem);
        }
        public void ResetAllTime()
        {
            Manager.ResetAllTime(LrcLinePanel);
        }
        public void ShiftAllTime(TimeSpan offset)
        {
            Manager.ShiftAllTime(LrcLinePanel, offset);
        }
        public void Undo()
        {
            Manager.Undo(LrcLinePanel);
        }
        public void Redo()
        {
            Manager.Redo(LrcLinePanel);
        }
        public void AddNewLine(TimeSpan time)
        {
            Manager.AddNewLine(LrcLinePanel, time);
        }
        public void DeleteLine()
        {
            Manager.DeleteLine(LrcLinePanel);
        }
        public void MoveUp()
        {
            Manager.MoveUp(LrcLinePanel);
        }
        public void MoveDown()
        {
            Manager.MoveDown(LrcLinePanel);
        }
		//自动高亮显示当前行
        public void ShowNextLine()
        {
            Manager.ShowNextLine(LrcLinePanel);
        }

        static void ResponseWrite()
        {
            ResponseWriteError();
        }
        static void ResponseWriteError()
        {
            //将错误信息写入日志
            Console.WriteLine(GetStackTraceModelName());
        }
        /// <summary>
        /// @Author:      HTL
        /// @Email:       Huangyuan413026@163.com
        /// @DateTime:    2015-06-03 19:54:49
        /// @Description: 获取当前堆栈的上级调用方法列表,直到最终调用者,只会返回调用的各方法,而不会返回具体的出错行数，可参考：微软真是个十足的混蛋啊！让我们跟踪Exception到行把！（不明真相群众请入） 
        /// </summary>
        /// <returns></returns>
        static string GetStackTraceModelName()
        {
            //当前堆栈信息
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame[] sfs = st.GetFrames();
            //过虑的方法名称,以下方法将不会出现在返回的方法调用列表中
            string _filterdName = "ResponseWrite,ResponseWriteError,";
            string _fullName = string.Empty, _methodName = string.Empty;
            for (int i = 1; i < sfs.Length; ++i)
            {
                //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) break;
                _methodName = sfs[i].GetMethod().Name;//方法名称
                                                      //sfs[i].GetFileLineNumber();//没有PDB文件的情况下将始终返回0
                if (_filterdName.Contains(_methodName)) continue;
                _fullName = _methodName + "()->" + _fullName;
            }
            st = null;
            sfs = null;
            _filterdName = _methodName = null;
            return _fullName.TrimEnd('-', '>');
        }


    }
}
