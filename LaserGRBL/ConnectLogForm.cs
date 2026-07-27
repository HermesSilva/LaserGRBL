//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaserGRBL
{
	/// <summary>
	/// Description of ConnectLogForm.
	/// </summary>
	public partial class ConnectLogForm : System.Windows.Forms.UserControl
	{
		private object[] baudRates = { 4800, 9600, 19200, 38400, 57600, 115200, 230400, 256000, 460800, 921600 };
		public ComWrapper.WrapperType currentWrapper;

		GrblCore Core;
		private string mLoadedFileName;

		//svg job power slider and size editor (created by code, added as new rows of tableLayoutPanel5, under the progress bar)
		private Label LblSvgPower;
		private TrackBar TBSvgPower;
		private Label LblSvgPowerValue;
		private Label LblSvgScale;
		private UserControls.NumericInput.NumericUpDown UDSvgScale;
		private Label LblSvgScaleValue;
		private Label LblSvgSpeed;
		private TrackBar TBSvgSpeed;
		private Label LblSvgSpeedValue;
		private bool mSuspendSvgPowerEvent;

		public ConnectLogForm()
		{
			currentWrapper = Settings.GetObject("ComWrapper Protocol", ComWrapper.WrapperType.UsbSerial);
			InitializeComponent();
        }

        public void SetCore(GrblCore core)
		{
			Core = core;
			Core.OnFileLoaded += OnFileLoaded;
			Core.OnFileChanged += OnFileLoaded;
			Core.OnLoopCountChange += OnLoopCountChanged;
			CmdLog.SetCom(core);

			PB.Bars.Add(new LaserGRBL.UserControls.DoubleProgressBar.Bar(ColorScheme.PreviewCommandWait));
			PB.Bars.Add(new LaserGRBL.UserControls.DoubleProgressBar.Bar(ColorScheme.PreviewCommandOK));
			Size btnSize = new Size(20, 20);
			IconsMgr.PrepareButton(BtnRunProgram, "mdi-play-circle", btnSize);
            IconsMgr.PrepareButton(BtnAbortProgram, "mdi-stop-circle", btnSize);
            IconsMgr.PrepareButton(BtnConnectDisconnect, "mdi-power-plug", btnSize, "mdi-power-plug-off");
            IconsMgr.PrepareButton(BtnOpen, "mdi-folder", btnSize);

            InitSpeedCB();
			InitPortCB();
			InitSvgPowerRow();

			RestoreConf();

			TimerUpdate();
		}

		private void InitSvgPowerRow()
		{
			LblSvgPower = new Label();
			LblSvgPower.Text = "Power";
			LblSvgPower.AutoSize = true;
			LblSvgPower.Anchor = AnchorStyles.Left;
			LblSvgPower.Margin = new Padding(3, 6, 3, 0);

			TBSvgPower = new TrackBar();
			TBSvgPower.AutoSize = false;
			TBSvgPower.Height = 26;
			TBSvgPower.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			TBSvgPower.TickStyle = TickStyle.None;
			TBSvgPower.Minimum = 0;
			TBSvgPower.Maximum = 1000;
			TBSvgPower.SmallChange = 1;
			TBSvgPower.LargeChange = 50;
			TBSvgPower.Margin = new Padding(3, 0, 3, 0);
			TBSvgPower.ValueChanged += TBSvgPower_ValueChanged;
			TBSvgPower.MouseUp += TBSvgPower_ApplyRequest;
			TBSvgPower.KeyUp += TBSvgPower_ApplyRequest;

			LblSvgPowerValue = new Label();
			LblSvgPowerValue.AutoSize = true;
			LblSvgPowerValue.Anchor = AnchorStyles.Left;
			LblSvgPowerValue.Margin = new Padding(3, 6, 3, 0);

			LblSvgScale = new Label();
			LblSvgScale.Text = "Scale";
			LblSvgScale.AutoSize = true;
			LblSvgScale.Anchor = AnchorStyles.Left;
			LblSvgScale.Margin = new Padding(3, 6, 3, 3);

			UDSvgScale = new UserControls.NumericInput.NumericUpDown();
			UDSvgScale.Anchor = AnchorStyles.Left;
			UDSvgScale.Width = 70;
			UDSvgScale.Minimum = GrblFile.SVG_SCALE_MIN; //negative values shrink the drawing, positive values enlarge it
			UDSvgScale.Maximum = GrblFile.SVG_SCALE_MAX;
			UDSvgScale.Increment = 1;
			UDSvgScale.Value = 0;
			UDSvgScale.Margin = new Padding(3, 3, 3, 3);
			UDSvgScale.ValueChanged += UDSvgScale_ValueChanged;

			LblSvgScaleValue = new Label();
			LblSvgScaleValue.AutoSize = true;
			LblSvgScaleValue.Anchor = AnchorStyles.Left;
			LblSvgScaleValue.Margin = new Padding(3, 6, 3, 3);

			LblSvgSpeed = new Label();
			LblSvgSpeed.Text = "Speed";
			LblSvgSpeed.AutoSize = true;
			LblSvgSpeed.Anchor = AnchorStyles.Left;
			LblSvgSpeed.Margin = new Padding(3, 6, 3, 0);

			TBSvgSpeed = new TrackBar();
			TBSvgSpeed.AutoSize = false;
			TBSvgSpeed.Height = 26;
			TBSvgSpeed.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			TBSvgSpeed.TickStyle = TickStyle.None;
			TBSvgSpeed.Minimum = GrblFile.SVG_SPEED_MIN; //-300 = one third of the programmed feed
			TBSvgSpeed.Maximum = GrblFile.SVG_SPEED_MAX; //+1 = programmed feed
			TBSvgSpeed.SmallChange = 1;
			TBSvgSpeed.LargeChange = 30;
			TBSvgSpeed.Value = GrblFile.SVG_SPEED_MAX;
			TBSvgSpeed.Margin = new Padding(3, 0, 3, 0);
			TBSvgSpeed.ValueChanged += TBSvgSpeed_ValueChanged;
			TBSvgSpeed.MouseUp += TBSvgSpeed_ApplyRequest;
			TBSvgSpeed.KeyUp += TBSvgSpeed_ApplyRequest;

			LblSvgSpeedValue = new Label();
			LblSvgSpeedValue.AutoSize = true;
			LblSvgSpeedValue.Anchor = AnchorStyles.Left;
			LblSvgSpeedValue.Margin = new Padding(3, 6, 3, 0);

			tableLayoutPanel5.SuspendLayout();
			tableLayoutPanel5.RowCount = 5;
			tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayoutPanel5.Controls.Add(LblSvgPower, 0, 2);
			tableLayoutPanel5.Controls.Add(TBSvgPower, 1, 2);
			tableLayoutPanel5.Controls.Add(LblSvgPowerValue, 2, 2);
			tableLayoutPanel5.SetColumnSpan(LblSvgPowerValue, 3);
			tableLayoutPanel5.Controls.Add(LblSvgScale, 0, 3);
			tableLayoutPanel5.Controls.Add(UDSvgScale, 1, 3);
			tableLayoutPanel5.Controls.Add(LblSvgScaleValue, 2, 3);
			tableLayoutPanel5.SetColumnSpan(LblSvgScaleValue, 3);
			tableLayoutPanel5.Controls.Add(LblSvgSpeed, 0, 4);
			tableLayoutPanel5.Controls.Add(TBSvgSpeed, 1, 4);
			tableLayoutPanel5.Controls.Add(LblSvgSpeedValue, 2, 4);
			tableLayoutPanel5.SetColumnSpan(LblSvgSpeedValue, 3);
			tableLayoutPanel5.ResumeLayout();

			SetSvgPowerRowVisible(false);
		}

		private void SetSvgPowerRowVisible(bool visible)
		{
			LblSvgPower.Visible = TBSvgPower.Visible = LblSvgPowerValue.Visible = visible;
			LblSvgSpeed.Visible = TBSvgSpeed.Visible = LblSvgSpeedValue.Visible = visible;
			LblSvgScale.Visible = UDSvgScale.Visible = LblSvgScaleValue.Visible = visible;
		}

		//called on file load: show the slider only for svg jobs and align it to the power of the loaded job
		private void UpdateSvgPowerRow()
		{
			bool visible = Core != null && Core.LoadedFile != null && Core.LoadedFile.CanChangeSvgLaserPower;

			if (visible && Core.LoadedFile.SvgTransformPending)
			{
				SetSvgPowerRowVisible(true); //a newer value is still being applied: do not pull controls back
				return;
			}

			if (visible)
			{
				int max = GrblCore.Configuration != null && GrblCore.Configuration.MaxPWM >= 1 ? (int)GrblCore.Configuration.MaxPWM : 1000;
				int value = Math.Max(0, Math.Min(max, Core.LoadedFile.SvgLaserPower));

				mSuspendSvgPowerEvent = true;
				TBSvgPower.Maximum = Math.Max(max, value);
				TBSvgPower.Value = value;
				TBSvgSpeed.Value = Math.Max(GrblFile.SVG_SPEED_MIN, Math.Min(GrblFile.SVG_SPEED_MAX, Core.LoadedFile.SvgSpeedValue));
				UDSvgScale.Value = Core.LoadedFile.SvgScalePercent;
				mSuspendSvgPowerEvent = false;

				RefreshSvgPowerLabel();
				RefreshSvgSpeedLabel();
				RefreshSvgScaleLabel();
			}

			SetSvgPowerRowVisible(visible);
		}

		private void RefreshSvgPowerLabel()
		{
			decimal maxpwm = GrblCore.Configuration != null ? GrblCore.Configuration.MaxPWM : -1;

			if (maxpwm > 0)
				LblSvgPowerValue.Text = string.Format("S{0} ({1})", TBSvgPower.Value, (TBSvgPower.Value / maxpwm).ToString("P1"));
			else
				LblSvgPowerValue.Text = string.Format("S{0}", TBSvgPower.Value);
		}

		private void RefreshSvgSpeedLabel()
		{
			decimal factor = GrblFile.SvgSpeedFactor(TBSvgSpeed.Value);
			LblSvgSpeedValue.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:P1} [{1:0.000}x]", factor, factor);
		}

		private void TBSvgSpeed_ValueChanged(object sender, EventArgs e)
		{
			if (mSuspendSvgPowerEvent)
				return;

			RefreshSvgSpeedLabel();
			TBSvgSpeed_ApplyRequest(sender, e); //requests are coalesced by GrblFile: safe to call on every step
		}

		private void TBSvgSpeed_ApplyRequest(object sender, EventArgs e)
		{
			if (mSuspendSvgPowerEvent || !TBSvgSpeed.Enabled || Core.LoadedFile == null)
				return;

			Core.LoadedFile.SetSvgSpeedValue(TBSvgSpeed.Value);
		}

		private void RefreshSvgScaleLabel()
		{
			decimal perc = UDSvgScale.Value;
			LblSvgScaleValue.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "% [{0:0.00}x]", (100 + perc) / 100m);
		}

		//size change rewrites all the coordinates of the job, but requests are coalesced by GrblFile,
		//so the drawing follows the editor without dropping any step
		private void UDSvgScale_ValueChanged(object sender, EventArgs e)
		{
			if (mSuspendSvgPowerEvent)
				return;

			RefreshSvgScaleLabel();

			if (!UDSvgScale.Enabled || Core.LoadedFile == null)
				return;

			Core.LoadedFile.SetSvgScalePercent((int)UDSvgScale.Value);
		}

		private void TBSvgPower_ValueChanged(object sender, EventArgs e)
		{
			if (mSuspendSvgPowerEvent)
				return;

			RefreshSvgPowerLabel();
			TBSvgPower_ApplyRequest(sender, e); //requests are coalesced by GrblFile: safe to call on every step
		}

		private void TBSvgPower_ApplyRequest(object sender, EventArgs e)
		{
			if (mSuspendSvgPowerEvent || !TBSvgPower.Enabled || Core.LoadedFile == null)
				return;

			Core.LoadedFile.SetSvgLaserPower(TBSvgPower.Value);
		}

		void OnLoopCountChanged(decimal current)
		{
			if (InvokeRequired)
			{
				Invoke(new GrblCore.dlgOnLoopCountChange(OnLoopCountChanged), current);
			}
			else
			{
				if (UDLoopCounter.Value != current)
					UDLoopCounter.Value = current;
			}
		}

		private void RestoreConf()
		{
			CBSpeed.SelectedItem = Settings.GetObject("Serial Speed", 115200);

			if (currentWrapper == ComWrapper.WrapperType.Telnet)
				TxtAddress.Text = Settings.GetObject("Telnet Address", "127.0.0.1:23");	
			else if (currentWrapper == ComWrapper.WrapperType.LaserWebESP8266)
				TxtAddress.Text = Settings.GetObject("Websocket URL", "ws://127.0.0.1:81/"); 
		}

		void OnFileLoaded(long elapsed, string filename)
		{
			if (InvokeRequired)
			{
				Invoke(new GrblFile.OnFileLoadedDlg(OnFileLoaded), elapsed, filename);
			}
			else
			{
				mLoadedFileName = filename;
				TbFileName.Text = System.IO.Path.GetFileName(filename);
				UpdateSvgPowerRow();
			}
		}

		private void InitSpeedCB() //Baud Rates combo box
		{
			CBSpeed.BeginUpdate();
			CBSpeed.Items.AddRange(baudRates);
			CBSpeed.EndUpdate();
		}

		private void InitPortCB() //Availabe Ports combo box
		{
			string currentport = CBPort.SelectedItem as string;
			CBPort.BeginUpdate();
			CBPort.Items.Clear();

			foreach (string portname in System.IO.Ports.SerialPort.GetPortNames())
			{
				string purgename = portname;

				//FIX https://github.com/arkypita/LaserGRBL/issues/31

				if (!char.IsDigit(purgename[purgename.Length - 1]))
					purgename = purgename.Substring(0, purgename.Length - 1);

				CBPort.Items.Add(purgename);
			}

			if (currentport != null && CBPort.Items.Contains(currentport))
				CBPort.SelectedItem = currentport;
			else if (CBPort.Items.Count > 0)
				CBPort.SelectedIndex = CBPort.Items.Count -1;
			CBPort.EndUpdate();
		}

		//private static System.Text.RegularExpressions.Regex ComRX = new System.Text.RegularExpressions.Regex(@"(?'wholecom'(?:^|[ (])COM(?'comno'\d+)(?:[) ]|$))", System.Text.RegularExpressions.RegexOptions.Compiled);
		//private System.Collections.Generic.SortedDictionary<int, string> GetPortDictionary()
		//{
		//	System.Collections.Generic.SortedDictionary<int, string> rv = new System.Collections.Generic.SortedDictionary<int, string>();
		//
		//	try //add using managment object
		//	{
		//		using (System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(@"\\.\root\cimv2", "SELECT * FROM Win32_PnPEntity"))
		//		{
		//			System.Management.ManagementObjectCollection moc = searcher.Get();
		//			foreach (System.Management.ManagementObject mo in moc)
		//			{
		//				string caption = (string)mo["Caption"];
		//				if (caption != null && ComRX.IsMatch(caption))
		//				{
		//					System.Text.RegularExpressions.Match m = ComRX.Match(caption);
		//					if (m != null && m.Groups["comno"] != null)
		//					{
		//						int no = int.Parse(m.Groups["comno"].Value);
		//						string wholecom = m.Groups["wholecom"].Value;
		//						if (!rv.ContainsKey(no))
		//							rv.Add(int.Parse(m.Groups["comno"].Value), caption.Replace(wholecom, "").Trim());
		//					}
		//				}
		//			}
		//
		//		}
		//	}
		//	catch { }
		//
		//	try //add using SerialPort.GetPortNames 
		//	{
		//		foreach (string dirty in System.IO.Ports.SerialPort.GetPortNames())
		//		{
		//			string comno = dirty;
		//			if (!char.IsDigit(comno[comno.Length - 1]))
		//				comno = comno.Substring(0, comno.Length - 1);
		//
		//			string caption = comno;
		//			if (caption != null && ComRX.IsMatch(caption))
		//			{
		//				System.Text.RegularExpressions.Match m = ComRX.Match(caption);
		//				if (m != null && m.Groups["comno"] != null)
		//				{
		//					int no = int.Parse(m.Groups["comno"].Value);
		//					if (!rv.ContainsKey(no))
		//						rv.Add(int.Parse(m.Groups["comno"].Value), "Generic COM Port");
		//				}
		//			}
		//		}
		//	}
		//	catch { }
		//
		//	return rv;
		//}


		void BtnConnectDisconnectClick(object sender, EventArgs e)
		{
			if (Core.MachineStatus == GrblCore.MacStatus.Disconnected)
				Core.OpenCom();
			else if (!(Core.InProgram && System.Windows.Forms.MessageBox.Show(Strings.DisconnectAnyway, Strings.WarnMessageBoxHeader, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes))
				Core.CloseCom(true);

			TimerUpdate();
		}


		void ApplyConfig()
		{
			if ((currentWrapper == ComWrapper.WrapperType.UsbSerial || currentWrapper == ComWrapper.WrapperType.UsbSerial2 || currentWrapper == ComWrapper.WrapperType.RJCPSerial) && CBPort.Text != null && CBSpeed.SelectedItem != null)
				Core.Configure(currentWrapper, CBPort.Text, (int)CBSpeed.SelectedItem);
			else if (currentWrapper == ComWrapper.WrapperType.Telnet || currentWrapper == ComWrapper.WrapperType.LaserWebESP8266)
				Core.Configure(currentWrapper, (string)TxtAddress.Text);
			else if (currentWrapper == ComWrapper.WrapperType.Emulator)
				Core.Configure(currentWrapper);
		}

		void BtnOpenClick(object sender, EventArgs e)
		{
			Core.OpenFile();
		}

		void BtnRunProgramClick(object sender, EventArgs e)
		{
			BtnRunProgram.Enabled = false;
			Core.RunProgram(ParentForm);
		}
		void TxtManualCommandCommandEntered(string command)
		{
			Core.EnqueueCommand(new GrblCommand(command, 0, true));
		}
		
		public void TimerUpdate()
		{
			SuspendLayout();

			if (!Core.IsConnected && System.IO.Ports.SerialPort.GetPortNames().Length != CBPort.Items.Count)
				InitPortCB();
			
			PB.Maximum = Core.ProgramTarget;
			PB.Bars[0].Value = Core.ProgramSent;
			PB.Bars[1].Value = Core.ProgramExecuted;

			string val = Tools.Utils.TimeSpanToString(Core.ProgramTime, Tools.Utils.TimePrecision.Minute, Tools.Utils.TimePrecision.Second, " ,", true);

			if (val != "now")
				PB.PercString = val;
			else if (Core.InProgram)
				PB.PercString = "0 sec";
			else
				PB.PercString = "";
			
			PB.Invalidate();

			/*
			Idle: All systems are go, no motions queued, and it's ready for anything.
			Run: Indicates a cycle is running.
			Hold: A feed hold is in process of executing, or slowing down to a stop. After the hold is complete, Grbl will remain in Hold and wait for a cycle start to resume the program.
			Door: (New in v0.9i) This compile-option causes Grbl to feed hold, shut-down the spindle and coolant, and wait until the door switch has been closed and the user has issued a cycle start. Useful for OEM that need safety doors.
			Home: In the middle of a homing cycle. NOTE: Positions are not updated live during the homing cycle, but they'll be set to the home position once done.
			Alarm: This indicates something has gone wrong or Grbl doesn't know its position. This state locks out all G-code commands, but allows you to interact with Grbl's settings if you need to. '$X' kill alarm lock releases this state and puts Grbl in the Idle state, which will let you move things again. As said before, be cautious of what you are doing after an alarm.
			Check: Grbl is in check G-code mode. It will process and respond to all G-code commands, but not motion or turn on anything. Once toggled off with another '$C' command, Grbl will reset itself.
			*/

			TT.SetToolTip(BtnConnectDisconnect, Core.IsConnected ? Strings.BtnDisconnectTT : Strings.BtnConnectTT);
			
			BtnConnectDisconnect.UseAltImage = Core.IsConnected;
			BtnRunProgram.Enabled = Core.CanSendFile;
            BtnRunProgram.Visible = !Core.CanAbortProgram;
            BtnAbortProgram.Visible = Core.CanAbortProgram;
            BtnOpen.Enabled = Core.CanLoadNewFile;
			if (TBSvgPower != null) //power, speed and size are fixed for the whole job: cannot be changed while running
				TBSvgPower.Enabled = TBSvgSpeed.Enabled = UDSvgScale.Enabled = !Core.InProgram;

			bool old = TxtManualCommand.Enabled;
			TxtManualCommand.Enabled = Core.CanSendManualCommand;
			//if (old == false && TxtManualCommand.Enabled == true)
			//	TxtManualCommand.Focus();

			//CBProtocol.Enabled = !Core.IsOpen;
			CBPort.Enabled = !Core.IsConnected;
			CBSpeed.Enabled = !Core.IsConnected;
			TxtAddress.Enabled = !Core.IsConnected;

			CmdLog.TimerUpdate();

			if (!Core.IsConnected)
			{
				ComWrapper.WrapperType actualWrapper = Settings.GetObject("ComWrapper Protocol", ComWrapper.WrapperType.UsbSerial);
				if (actualWrapper != currentWrapper)
				{
					currentWrapper = actualWrapper;
					UpdateConf();
				}
			}

			ResumeLayout();
		}

		private void CBPort_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateConf();
		}

		private void CBPort_TextChanged(object sender, EventArgs e)
		{
			UpdateConf();
		}

		private void CBSpeed_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateConf();
		}

		
		private void UpdateConf()
		{
			tableLayoutPanel4.SuspendLayout();
			CBPort.Visible = CBSpeed.Visible = LblComPort.Visible = LblBaudRate.Visible = (currentWrapper == ComWrapper.WrapperType.UsbSerial || currentWrapper == ComWrapper.WrapperType.UsbSerial2 || currentWrapper == ComWrapper.WrapperType.RJCPSerial);
			TxtAddress.Visible = LblAddress.Visible = (currentWrapper == ComWrapper.WrapperType.Telnet || currentWrapper == ComWrapper.WrapperType.LaserWebESP8266);
			LblAddress.Text = (currentWrapper == ComWrapper.WrapperType.Telnet ? "IP:PORT" : "Socket URL");
			TxtEmulator.Visible = LblEmulator.Visible = (currentWrapper == ComWrapper.WrapperType.Emulator);
			tableLayoutPanel4.ResumeLayout();

			if (CBSpeed.SelectedItem != null)
				Settings.SetObject("Serial Speed", CBSpeed.SelectedItem);

			if (!string.IsNullOrWhiteSpace(TxtAddress.Text))
			{
				if (currentWrapper == ComWrapper.WrapperType.Telnet)
					Settings.SetObject("Telnet Address", TxtAddress.Text);
				else if (currentWrapper == ComWrapper.WrapperType.LaserWebESP8266)
					Settings.SetObject("Websocket URL", TxtAddress.Text);
			}

			ApplyConfig();
		}

		private void CBProtocol_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateConf();
		}

		private void TxtHostName_TextChanged(object sender, EventArgs e)
		{
			UpdateConf();
		}

		private void ITcpPort_CurrentValueChanged(object sender, int NewValue, bool ByUser)
		{
			UpdateConf();
		}

		private void UDLoopCounter_ValueChanged(object sender, EventArgs e)
		{
			Core.LoopCount = UDLoopCounter.Value;
		}

		internal void OnColorChange()
		{
			TbFileName.BackColor = ColorScheme.LogBackColor;
            TbFileName.ForeColor = ColorScheme.FormForeColor;

            TbFileName.BorderColor = ColorScheme.ControlsBorder;
            TbFileName.ForeColor = ColorScheme.FormForeColor;

            TxtManualCommand.WaterMarkColor = ColorScheme.ControlsBorder;
            TxtManualCommand.WaterMarkActiveColor = ColorScheme.ControlsBorder;
            TxtManualCommand.BackColor = ColorScheme.LogBackColor;
			TxtManualCommand.ForeColor = ColorScheme.FormForeColor;

			PB.ForeColor = ColorScheme.FormForeColor;
            PB.FillColor = ColorScheme.LogBackColor;
            PB.Bars.Clear();
			PB.Bars.Add(new LaserGRBL.UserControls.DoubleProgressBar.Bar(ColorScheme.PreviewCommandWait));
			PB.Bars.Add(new LaserGRBL.UserControls.DoubleProgressBar.Bar(ColorScheme.PreviewCommandOK));
			if (LblSvgPower != null)
			{
				LblSvgPower.ForeColor = ColorScheme.FormForeColor;
				LblSvgPowerValue.ForeColor = ColorScheme.FormForeColor;
				TBSvgPower.BackColor = ColorScheme.FormBackColor;
				LblSvgSpeed.ForeColor = ColorScheme.FormForeColor;
				LblSvgSpeedValue.ForeColor = ColorScheme.FormForeColor;
				TBSvgSpeed.BackColor = ColorScheme.FormBackColor;
				LblSvgScale.ForeColor = ColorScheme.FormForeColor;
				LblSvgScaleValue.ForeColor = ColorScheme.FormForeColor;
			}

            CmdLog.OnColorChange();
            CmdLog.Invalidate();
		}

		private void TxtManualCommand_Enter(object sender, EventArgs e)
		{
			Core.SuspendHK = true;
		}

		private void TxtManualCommand_Leave(object sender, EventArgs e)
		{
			Core.SuspendHK = false;
		}

		private void TbFileName_MouseEnter(object sender, EventArgs e)
		{
			if (mLoadedFileName != null)
				TT.Show(mLoadedFileName, TbFileName, 5000);
		}

		private void TbFileName_MouseLeave(object sender, EventArgs e)
		{
			TT.Hide(TbFileName);
		}

        private void BtnAbortProgram_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.BoxAbortProgramConfirm, Strings.WarnMessageBoxHeader, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
                Core.AbortProgram();
        }

		internal void ConfigFromDiscovery(string config)
		{
			if (TxtAddress.Visible && TxtAddress.Enabled && config != null)
			{
				TxtAddress.Text = config;
				Application.DoEvents();

				if (BtnConnectDisconnect.Enabled && Core.MachineStatus == GrblCore.MacStatus.Disconnected)
					BtnConnectDisconnectClick(null, null);
			}
		}

		internal void ConfigFromWiFiForm(string config)
		{
			if (config != null)
			{
				TxtAddress.Text = config;
				Application.DoEvents();

				//if (BtnConnectDisconnect.Enabled && Core.MachineStatus == GrblCore.MacStatus.Disconnected)
				//	BtnConnectDisconnectClick(null, null);
			}
        }

        private void CmdLog_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, e.ClipRectangle, ColorScheme.ControlsBorder, ButtonBorderStyle.Solid);
        }

    }
}
