using ASCOM.Utilities;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ASCOM.photonWanderer.Rotator
{
    [ComVisible(false)] // Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        const string NO_PORTS_MESSAGE = "No COM ports found";
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void CmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            try
            {
                if (numericBacklash.Value < 0)
                {
                    throw new InvalidOperationException("Backlash must be 0.0 degrees or greater.");
                }

                if (numericCompletionCorrectionThreshold.Value <= 0)
                {
                    throw new InvalidOperationException("Correction threshold must be greater than 0.0 degrees.");
                }

                if (numericDefaultMotionRate.Value <= 0)
                {
                    throw new InvalidOperationException("Default motion rate must be greater than 0.0 degrees per second.");
                }

                tl.Enabled = chkTrace.Checked;

                if (comboBoxComPort.SelectedItem is null)
                {
                    tl.LogMessage("Setup OK", "New configuration values - COM Port: Not selected");
                }
                else if (comboBoxComPort.SelectedItem.ToString() == NO_PORTS_MESSAGE)
                {
                    tl.LogMessage("Setup OK", "New configuration values - NO COM ports detected on this PC.");
                }
                else
                {
                    RotatorHardware.comPort = (string)comboBoxComPort.SelectedItem;
                    tl.LogMessage("Setup OK", $"New configuration values - COM Port: {comboBoxComPort.SelectedItem}");
                }

                RotatorHardware.Reverse = chkReverse.Checked;
                RotatorHardware.Backlash = (float)numericBacklash.Value;
                RotatorHardware.CompletionCorrectionThresholdDegrees = (float)numericCompletionCorrectionThreshold.Value;
                RotatorHardware.DefaultMotionRateDegreesPerSecond = (float)numericDefaultMotionRate.Value;
                RefreshMeasuredMotionRateDisplay();

                tl.LogMessage(
                    "Setup OK",
                    $"New configuration values - Reverse: {chkReverse.Checked}, Backlash: {numericBacklash.Value.ToString(CultureInfo.InvariantCulture)}, Correction threshold: {numericCompletionCorrectionThreshold.Value.ToString(CultureInfo.InvariantCulture)}, Default motion rate: {numericDefaultMotionRate.Value.ToString(CultureInfo.InvariantCulture)}, Measured motion rate: {textMeasuredDegreesPerSecond.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to apply setup values", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }

        private void CmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            chkTrace.Checked = tl.Enabled;
            chkReverse.Checked = RotatorHardware.Reverse;
            decimal backlash = Convert.ToDecimal(RotatorHardware.Backlash, CultureInfo.InvariantCulture);
            decimal correctionThreshold = Convert.ToDecimal(RotatorHardware.CompletionCorrectionThresholdDegrees, CultureInfo.InvariantCulture);
            decimal defaultMotionRate = Convert.ToDecimal(RotatorHardware.DefaultMotionRateDegreesPerSecond, CultureInfo.InvariantCulture);
            numericBacklash.Value = Math.Min(numericBacklash.Maximum, Math.Max(numericBacklash.Minimum, backlash));
            numericCompletionCorrectionThreshold.Value = Math.Min(numericCompletionCorrectionThreshold.Maximum, Math.Max(numericCompletionCorrectionThreshold.Minimum, correctionThreshold));
            numericDefaultMotionRate.Value = Math.Min(numericDefaultMotionRate.Maximum, Math.Max(numericDefaultMotionRate.Minimum, defaultMotionRate));
            RefreshMeasuredMotionRateDisplay();
            buttonSetZero.Enabled = RotatorHardware.IsConnectedForSetup;
            comboBoxComPort.Enabled = !RotatorHardware.IsConnectedForSetup;

            comboBoxComPort.Items.Clear();
            using (Serial serial = new Serial())
            {
                comboBoxComPort.Items.AddRange(serial.AvailableCOMPorts);
            }

            if (comboBoxComPort.Items.Count == 0)
            {
                comboBoxComPort.Items.Add(NO_PORTS_MESSAGE);
                comboBoxComPort.SelectedItem = NO_PORTS_MESSAGE;
            }

            if (comboBoxComPort.Items.Contains(RotatorHardware.comPort))
            {
                comboBoxComPort.SelectedItem = RotatorHardware.comPort;
            }
            else if (comboBoxComPort.Items.Count > 0)
            {
                comboBoxComPort.SelectedIndex = 0;
            }

            tl.LogMessage(
                "InitUI",
                $"Set UI controls to Trace: {chkTrace.Checked}, COM Port: {comboBoxComPort.SelectedItem}, Reverse: {chkReverse.Checked}, Backlash: {numericBacklash.Value}, Correction threshold: {numericCompletionCorrectionThreshold.Value}, Default motion rate: {numericDefaultMotionRate.Value}, Measured motion rate: {textMeasuredDegreesPerSecond.Text}");
        }

        private void RefreshMeasuredMotionRateDisplay()
        {
            textMeasuredDegreesPerSecond.Text = RotatorHardware.MeasuredDegreesPerSecond.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {
            // Bring the setup dialogue to the front of the screen
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            else
            {
                TopMost = true;
                Focus();
                BringToFront();
                TopMost = false;
            }
        }

        private void ButtonSetZero_Click(object sender, EventArgs e)
        {
            try
            {
                RotatorHardware.SetMechanicalZero();
                MessageBox.Show("The rotator mechanical position has been reset to zero.", "WandererRotator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to set zero", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}