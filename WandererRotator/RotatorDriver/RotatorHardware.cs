using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASCOM.photonWanderer.Rotator
{
    [HardwareClass()]
    internal static class RotatorHardware
    {
        internal const string comPortProfileName = "COM Port";
        internal const string comPortDefault = "COM1";
        internal const string traceStateProfileName = "Trace Level";
        internal const string traceStateDefault = "true";
        internal const string reverseProfileName = "Reverse";
        internal const string reverseDefault = "false";
        internal const string backlashProfileName = "Backlash";
        internal const string backlashDefault = "0.5";
        internal const string completionCorrectionThresholdProfileName = "Completion Correction Threshold";
        internal const string completionCorrectionThresholdDefault = "0.01";
        internal const string defaultMotionRateProfileName = "Default Motion Rate";
        internal const string defaultMotionRateDefault = "3.5";
        internal const string virtualMechanicalPositionProfileName = "Virtual Mechanical Position";
        internal const string virtualMechanicalPositionDefault = "0.0";

        private const string HandshakeCommand = "1500001";
        private const string StopCommand = "stop";
        private const string ReverseNormalCommand = "1700000";
        private const string ReverseReversedCommand = "1700001";
        private const string LowVoltageResponse = "NP";
        private const double MoveCommandScale = 1000.0;
        private const double BacklashCommandOffset = 1600000.0;
        private const double BacklashCommandScale = 10.0;
        private const float MinimumMoveDegrees = 0.01f;
        private const float MinimumStepDegrees = (float)(1.0 / MoveCommandScale);
        private const int MaximumConnectAttempts = 3;
        private const int MaximumCorrectionAttempts = 3;
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan MoveCompletionTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MoveCompletionGracePeriod = TimeSpan.FromMilliseconds(750);
        private const int SerialReceiveTimeoutMs = 250;

        private static readonly Regex HandshakeRegex = new Regex(
            @"^(?<model>[A-Za-z0-9]+)A(?<firmware>\d+)A(?<mechanical>-?\d+)A(?<backlash>\d+(?:\.\d+)?)A(?<reverse>[01])A?$",
            RegexOptions.Compiled);

        private static readonly Regex MoveCompletionRegex = new Regex(
            @"(?<delta>-?\d+(?:\.\d+)?)A(?<mechanical>-?\d{4,6})A",
            RegexOptions.Compiled);

        private static readonly object serialLock = new object();
        private static readonly object stateLock = new object();

        private static string DriverProgId = "";
        private static string DriverDescription = "";
        internal static string comPort;
        private static bool connectedState;
        private static bool runOnce;
        internal static Util utilities;
        internal static AstroUtils astroUtilities;
        internal static TraceLogger tl;

        private static Serial serialPort;
        private static bool reverseState;
        private static float backlashValue;
        private static string firmwareVersion = "Unknown";
        private static string deviceModel = "WanderRotator";
        private static float firmwareMechanicalPosition;
        private static float virtualMechanicalPosition;
        private static float syncOffset;
        private static float targetPosition;
        private static bool isMoving;
        private static float completionCorrectionThresholdDegrees = 0.01f;
        private static float defaultMotionRateDegreesPerSecond = 3.5f;
        private static float estimatedDegreesPerSecond = 3.5f;
        private static float activeMoveStartVirtualMechanicalPosition;
        private static float activeMoveStartFirmwareMechanicalPosition;
        private static float activeMoveSignedDegrees;
        private static float activeMoveTargetVirtualMechanicalPosition;
        private static float activeMoveTargetFirmwareMechanicalPosition;
        private static DateTime activeMoveStartUtc;
        private static bool waitingForCompletionFrame;
        private static Task activeMoveTask;
        private static CancellationTokenSource moveCancellation;

        private static readonly List<Guid> uniqueIds = new List<Guid>();

        static RotatorHardware()
        {
            try
            {
                tl = new TraceLogger("", "photonWanderer.Hardware");
                DriverProgId = Rotator.DriverProgId;
                ReadProfile();
                LogMessage("RotatorHardware", "Static initialiser completed.");
            }
            catch (Exception ex)
            {
                try { LogMessage("RotatorHardware", $"Initialisation exception: {ex}"); } catch { }
                MessageBox.Show($"RotatorHardware - {ex.Message}\r\n{ex}", $"Exception creating {Rotator.DriverProgId}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        internal static void InitialiseHardware()
        {
            LogMessage("InitialiseHardware", "Start.");

            if (!runOnce)
            {
                LogMessage("InitialiseHardware", "Starting one-off initialisation.");

                DriverDescription = Rotator.DriverDescription;
                connectedState = false;
                utilities = new Util();
                astroUtilities = new AstroUtils();
                firmwareMechanicalPosition = 0.0f;
                syncOffset = 0.0f;
                targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                isMoving = false;
                estimatedDegreesPerSecond = defaultMotionRateDegreesPerSecond;

                LogMessage("InitialiseHardware", $"ProgID: {DriverProgId}, Description: {DriverDescription}");
                LogMessage("InitialiseHardware", "Completed basic initialisation");
                LogMessage("InitialiseHardware", "One-off initialisation complete.");

                runOnce = true;
            }
        }

        #region Common properties and methods.

        public static void SetupDialog()
        {
            using (SetupDialogForm form = new SetupDialogForm(tl))
            {
                DialogResult result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    WriteProfile();
                }
            }
        }

        public static ArrayList SupportedActions
        {
            get
            {
                LogMessage("SupportedActions Get", "Returning empty ArrayList");
                return new ArrayList();
            }
        }

        public static string Action(string actionName, string actionParameters)
        {
            LogMessage("Action", $"Action {actionName}, parameters {actionParameters} is not implemented");
            throw new ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public static void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new MethodNotImplementedException($"CommandBlind - Command:{command}, Raw: {raw}.");
        }

        public static bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new MethodNotImplementedException($"CommandBool - Command:{command}, Raw: {raw}.");
        }

        public static string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            throw new MethodNotImplementedException($"CommandString - Command:{command}, Raw: {raw}.");
        }

        public static void Dispose()
        {
            try { LogMessage("Dispose", "Disposing of assets and closing down."); } catch { }

            try
            {
                if (ConnectionActive)
                {
                    try
                    {
                        StopActiveMoveInternal(true);
                    }
                    catch
                    {
                    }

                    CloseSerialConnection();
                }
            }
            catch
            {
            }

            try
            {
                if (tl != null)
                {
                    tl.Enabled = false;
                    tl.Dispose();
                    tl = null;
                }
            }
            catch
            {
            }

            try
            {
                if (utilities != null)
                {
                    utilities.Dispose();
                    utilities = null;
                }
            }
            catch
            {
            }

            try
            {
                if (astroUtilities != null)
                {
                    astroUtilities.Dispose();
                    astroUtilities = null;
                }
            }
            catch
            {
            }
        }

        public static void SetConnected(Guid uniqueId, bool newState)
        {
            if (newState)
            {
                if (uniqueIds.Contains(uniqueId))
                {
                    LogMessage("SetConnected", "Ignoring request to connect because the device is already connected.");
                    return;
                }

                if (uniqueIds.Count == 0)
                {
                    LogMessage("SetConnected", "Connecting to hardware.");
                    OpenAndInitialiseHardware();
                }
                else
                {
                    LogMessage("SetConnected", "Hardware already connected.");
                }

                uniqueIds.Add(uniqueId);
                LogMessage("SetConnected", $"Unique id {uniqueId} added to the connection list.");
            }
            else
            {
                if (!uniqueIds.Contains(uniqueId))
                {
                    LogMessage("SetConnected", "Ignoring request to disconnect because the device is already disconnected.");
                    return;
                }

                uniqueIds.Remove(uniqueId);
                LogMessage("SetConnected", $"Unique id {uniqueId} removed from the connection list.");

                if (uniqueIds.Count == 0)
                {
                    LogMessage("SetConnected", "Disconnecting from hardware.");

                    try
                    {
                        StopActiveMoveInternal(true);
                    }
                    catch (Exception ex)
                    {
                        LogMessage("SetConnected", $"Stop during disconnect failed: {ex.Message}");
                    }

                    CloseSerialConnection();
                }
                else
                {
                    LogMessage("SetConnected", "Hardware remains connected for other driver instances.");
                }
            }

            LogMessage("SetConnected", "Currently connected driver ids:");
            foreach (Guid id in uniqueIds)
            {
                LogMessage("SetConnected", $" ID {id} is connected");
            }
        }

        public static string Description
        {
            get
            {
                LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        public static string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = $"{deviceModel} driver. Firmware: {firmwareVersion}. Version: {version.Major}.{version.Minor}";
                LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public static string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = $"{version.Major}.{version.Minor}";
                LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public static short InterfaceVersion
        {
            get
            {
                LogMessage("InterfaceVersion Get", "4");
                return Convert.ToInt16("4");
            }
        }

        public static string Name
        {
            get
            {
                const string name = "WandererRotator";
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IRotator Implementation

        internal static bool CanReverse
        {
            get
            {
                LogMessage("CanReverse Get", true.ToString());
                return true;
            }
        }

        internal static void Halt()
        {
            CheckConnected("Halt");
            StopActiveMoveInternal(true);
        }

        internal static bool IsMoving
        {
            get
            {
                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    LogMessage("IsMoving Get", isMoving.ToString());
                    return isMoving;
                }
            }
        }

        internal static void Move(float position)
        {
            CheckConnected("Move");

            float currentLogicalPosition = Position;
            float newTarget = NormalizeAngle(currentLogicalPosition + position);

            LogMessage("Move", $"Relative move {position} -> target {newTarget}");
            MoveAbsolute(newTarget);
        }

        internal static void MoveAbsolute(float position)
        {
            CheckConnected("MoveAbsolute");

            float requestedTarget = NormalizeAngle(position);
            float deltaDegrees;

            lock (stateLock)
            {
                UpdateEstimatedMotionStateLocked();
                deltaDegrees = ComputeCableSafeDeltaLocked(ConvertLogicalToVirtualMechanicalTargetLocked(requestedTarget));
            }

            if (Math.Abs(deltaDegrees) < MinimumMoveDegrees)
            {
                lock (stateLock)
                {
                    targetPosition = requestedTarget;
                }

                LogMessage("MoveAbsolute", $"Ignoring negligible move to {requestedTarget}");
                return;
            }

            StopActiveMoveInternal(true);
            StartMove(deltaDegrees, requestedTarget);
        }

        internal static float Position
        {
            get
            {
                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    float position = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                    LogMessage("Position Get", position.ToString(CultureInfo.InvariantCulture));
                    return position;
                }
            }
        }

        internal static bool Reverse
        {
            get
            {
                lock (stateLock)
                {
                    LogMessage("Reverse Get", reverseState.ToString());
                    return reverseState;
                }
            }
            set
            {
                bool applyImmediately;

                lock (stateLock)
                {
                    reverseState = value;
                    applyImmediately = connectedState;
                }

                LogMessage("Reverse Set", value.ToString());

                if (applyImmediately)
                {
                    ApplyReverseSetting();
                }
            }
        }

        internal static float StepSize
        {
            get
            {
                LogMessage("StepSize Get", MinimumStepDegrees.ToString(CultureInfo.InvariantCulture));
                return MinimumStepDegrees;
            }
        }

        internal static float TargetPosition
        {
            get
            {
                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    float currentTarget = isMoving ? targetPosition : NormalizeAngle(virtualMechanicalPosition + syncOffset);
                    LogMessage("TargetPosition Get", currentTarget.ToString(CultureInfo.InvariantCulture));
                    return currentTarget;
                }
            }
        }

        internal static float MechanicalPosition
        {
            get
            {
                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    LogMessage("MechanicalPosition Get", virtualMechanicalPosition.ToString(CultureInfo.InvariantCulture));
                    return virtualMechanicalPosition;
                }
            }
        }

        internal static void MoveMechanical(float position)
        {
            CheckConnected("MoveMechanical");

            float requestedMechanicalPosition = NormalizeAngle(position);
            float deltaDegrees;

            lock (stateLock)
            {
                UpdateEstimatedMotionStateLocked();
                deltaDegrees = ComputeCableSafeDeltaLocked(requestedMechanicalPosition);
            }

            if (Math.Abs(deltaDegrees) < MinimumMoveDegrees)
            {
                lock (stateLock)
                {
                    targetPosition = NormalizeAngle(requestedMechanicalPosition + syncOffset);
                }

                LogMessage("MoveMechanical", $"Ignoring negligible move to mechanical position {requestedMechanicalPosition}");
                return;
            }

            StopActiveMoveInternal(true);
            StartMove(deltaDegrees, NormalizeAngle(requestedMechanicalPosition + syncOffset));
        }

        internal static void Sync(float position)
        {
            CheckConnected("Sync");

            lock (stateLock)
            {
                UpdateEstimatedMotionStateLocked();
                syncOffset = NormalizeAngle(position - virtualMechanicalPosition);
                targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                LogMessage("Sync", $"Synced logical position to {position}, sync offset is now {syncOffset}");
            }
        }

        #endregion

        #region Private properties and methods

        internal static bool IsConnectedForSetup
        {
            get { return ConnectionActive; }
        }

        internal static float Backlash
        {
            get
            {
                lock (stateLock)
                {
                    return backlashValue;
                }
            }
            set
            {
                if (value < 0.0f)
                {
                    throw new InvalidValueException("Backlash", value.ToString(CultureInfo.InvariantCulture), "0 or greater");
                }

                bool applyImmediately;

                lock (stateLock)
                {
                    backlashValue = value;
                    applyImmediately = connectedState;
                }

                LogMessage("Backlash Set", value.ToString(CultureInfo.InvariantCulture));

                if (applyImmediately)
                {
                    ApplyBacklashSetting();
                }
            }
        }

        internal static float CompletionCorrectionThresholdDegrees
        {
            get
            {
                lock (stateLock)
                {
                    return completionCorrectionThresholdDegrees;
                }
            }
            set
            {
                if (value < MinimumStepDegrees)
                {
                    throw new InvalidValueException("CompletionCorrectionThresholdDegrees", value.ToString(CultureInfo.InvariantCulture), $"at least {MinimumStepDegrees.ToString(CultureInfo.InvariantCulture)}");
                }

                lock (stateLock)
                {
                    completionCorrectionThresholdDegrees = value;
                }

                LogMessage("CompletionCorrectionThresholdDegrees Set", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        internal static float DefaultMotionRateDegreesPerSecond
        {
            get
            {
                lock (stateLock)
                {
                    return defaultMotionRateDegreesPerSecond;
                }
            }
            set
            {
                if (value < MinimumStepDegrees)
                {
                    throw new InvalidValueException("DefaultMotionRateDegreesPerSecond", value.ToString(CultureInfo.InvariantCulture), $"at least {MinimumStepDegrees.ToString(CultureInfo.InvariantCulture)}");
                }

                lock (stateLock)
                {
                    defaultMotionRateDegreesPerSecond = value;
                    estimatedDegreesPerSecond = value;
                }

                LogMessage("DefaultMotionRateDegreesPerSecond Set", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        internal static float MeasuredDegreesPerSecond
        {
            get
            {
                lock (stateLock)
                {
                    return estimatedDegreesPerSecond;
                }
            }
        }

        internal static float VirtualMechanicalPosition
        {
            get
            {
                lock (stateLock)
                {
                    return virtualMechanicalPosition;
                }
            }
            set
            {
                float normalizedValue = NormalizeAngle(value);

                lock (stateLock)
                {
                    virtualMechanicalPosition = normalizedValue;

                    if (!isMoving)
                    {
                        targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                    }
                }

                PersistVirtualMechanicalPosition(normalizedValue);
                LogMessage("VirtualMechanicalPosition Set", normalizedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static bool IsConnected
        {
            get { return ConnectionActive; }
        }

        private static bool ConnectionActive
        {
            get
            {
                lock (stateLock)
                {
                    return connectedState && serialPort != null && serialPort.Connected;
                }
            }
        }

        private static void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException(message);
            }
        }

        internal static void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Rotator";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, traceStateProfileName, string.Empty, traceStateDefault), CultureInfo.InvariantCulture);
                comPort = driverProfile.GetValue(DriverProgId, comPortProfileName, string.Empty, comPortDefault);
                reverseState = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, reverseProfileName, string.Empty, reverseDefault), CultureInfo.InvariantCulture);
                backlashValue = ParseFloat(driverProfile.GetValue(DriverProgId, backlashProfileName, string.Empty, backlashDefault), backlashDefault);
                completionCorrectionThresholdDegrees = Math.Max(
                    MinimumStepDegrees,
                    ParseFloat(
                        driverProfile.GetValue(DriverProgId, completionCorrectionThresholdProfileName, string.Empty, completionCorrectionThresholdDefault),
                        completionCorrectionThresholdDefault));
                defaultMotionRateDegreesPerSecond = Math.Max(
                    MinimumStepDegrees,
                    ParseFloat(
                        driverProfile.GetValue(DriverProgId, defaultMotionRateProfileName, string.Empty, defaultMotionRateDefault),
                        defaultMotionRateDefault));
                virtualMechanicalPosition = NormalizeAngle(
                    ParseFloat(
                        driverProfile.GetValue(DriverProgId, virtualMechanicalPositionProfileName, string.Empty, virtualMechanicalPositionDefault),
                        virtualMechanicalPositionDefault));
                estimatedDegreesPerSecond = defaultMotionRateDegreesPerSecond;
            }
        }

        internal static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Rotator";
                driverProfile.WriteValue(DriverProgId, traceStateProfileName, tl.Enabled.ToString(CultureInfo.InvariantCulture));
                driverProfile.WriteValue(DriverProgId, comPortProfileName, comPort ?? comPortDefault);
                driverProfile.WriteValue(DriverProgId, reverseProfileName, reverseState.ToString(CultureInfo.InvariantCulture));
                driverProfile.WriteValue(DriverProgId, backlashProfileName, backlashValue.ToString(CultureInfo.InvariantCulture));
                driverProfile.WriteValue(DriverProgId, completionCorrectionThresholdProfileName, completionCorrectionThresholdDegrees.ToString(CultureInfo.InvariantCulture));
                driverProfile.WriteValue(DriverProgId, defaultMotionRateProfileName, defaultMotionRateDegreesPerSecond.ToString(CultureInfo.InvariantCulture));
                driverProfile.WriteValue(DriverProgId, virtualMechanicalPositionProfileName, virtualMechanicalPosition.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void PersistVirtualMechanicalPosition(float value)
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Rotator";
                driverProfile.WriteValue(DriverProgId, virtualMechanicalPositionProfileName, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        internal static void LogMessage(string identifier, string message)
        {
            if (tl != null)
            {
                tl.LogMessageCrLf(identifier, message);
            }
        }

        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, message, args);
            LogMessage(identifier, msg);
        }

        private static void OpenAndInitialiseHardware()
        {
            if (string.IsNullOrWhiteSpace(comPort))
            {
                throw new DriverException("No COM port has been configured for the WandererRotator driver.");
            }

            Exception lastException = null;

            for (int attempt = 1; attempt <= MaximumConnectAttempts; attempt++)
            {
                try
                {
                    lock (serialLock)
                    {
                        if (serialPort == null)
                        {
                            serialPort = new Serial();
                        }

                        serialPort.PortName = comPort;
                        serialPort.Speed = SerialSpeed.ps19200;
                        serialPort.DataBits = 8;
                        serialPort.Parity = SerialParity.None;
                        serialPort.StopBits = SerialStopBits.One;
                        serialPort.Handshake = SerialHandshake.None;
                        serialPort.DTREnable = false;
                        serialPort.RTSEnable = false;
                        serialPort.ReceiveTimeoutMs = SerialReceiveTimeoutMs;
                        serialPort.Connected = true;
                        serialPort.ClearBuffers();
                    }

                    HandshakeResult handshakeResult = ParseHandshakeResponse(
                        SendCommandAndReceive(HandshakeCommand, IsHandshakeComplete, HandshakeTimeout, CancellationToken.None));

                    lock (stateLock)
                    {
                        deviceModel = handshakeResult.Model;
                        firmwareVersion = handshakeResult.FirmwareVersion;
                        firmwareMechanicalPosition = NormalizeAngle(handshakeResult.MechanicalDegrees);
                        syncOffset = 0.0f;
                        targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                        connectedState = true;
                    }

                    LogMessage(
                        "SetConnected",
                        $"Handshake OK on attempt {attempt} of {MaximumConnectAttempts}. Model {handshakeResult.Model}, firmware {handshakeResult.FirmwareVersion}, firmware mechanical {handshakeResult.MechanicalDegrees}, virtual mechanical {virtualMechanicalPosition}, backlash {handshakeResult.BacklashDegrees}, reverse {handshakeResult.Reverse}");

                    if (Math.Abs(handshakeResult.BacklashDegrees - Backlash) > 0.001f)
                    {
                        LogMessage("SetConnected", $"Applying persisted backlash {Backlash} over device value {handshakeResult.BacklashDegrees}.");
                        ApplyBacklashSetting();
                    }

                    if (handshakeResult.Reverse != Reverse)
                    {
                        LogMessage("SetConnected", $"Applying persisted reverse {Reverse} over device value {handshakeResult.Reverse}.");
                        ApplyReverseSetting();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogMessage("SetConnected", $"Connection attempt {attempt} of {MaximumConnectAttempts} failed: {ex.Message}");
                    CloseSerialConnection();

                    if (attempt < MaximumConnectAttempts)
                    {
                        Thread.Sleep(200);
                    }
                }
            }

            throw new DriverException($"Failed to connect to the rotator after {MaximumConnectAttempts} attempts.", lastException);
        }

        private static void CloseSerialConnection()
        {
            lock (stateLock)
            {
                connectedState = false;
                isMoving = false;
                targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                ResetActiveMoveEstimateLocked();
            }

            CancellationTokenSource cancellationSource;
            Task moveTask;

            lock (stateLock)
            {
                cancellationSource = moveCancellation;
                moveTask = activeMoveTask;
                moveCancellation = null;
                activeMoveTask = null;
            }

            if (cancellationSource != null)
            {
                try
                {
                    cancellationSource.Cancel();
                    cancellationSource.Dispose();
                }
                catch
                {
                }
            }

            if (moveTask != null)
            {
                try
                {
                    moveTask.Wait(500);
                }
                catch
                {
                }
            }

            lock (serialLock)
            {
                if (serialPort != null)
                {
                    try
                    {
                        if (serialPort.Connected)
                        {
                            serialPort.ClearBuffers();
                            serialPort.Connected = false;
                        }
                    }
                    finally
                    {
                        serialPort.Dispose();
                        serialPort = null;
                    }
                }
            }
        }

        private static void ApplyReverseSetting()
        {
            string command = Reverse ? ReverseReversedCommand : ReverseNormalCommand;
            SendBlindCommand(command, true);
        }

        private static void ApplyBacklashSetting()
        {
            int backlashCommand = (int)Math.Round(
                BacklashCommandOffset + (Backlash * (float)BacklashCommandScale),
                MidpointRounding.AwayFromZero);

            SendBlindCommand(backlashCommand.ToString(CultureInfo.InvariantCulture), true);
        }

        private static void StartMove(float logicalDeltaDegrees, float logicalTarget)
        {
            string command = CreateMoveCommand(logicalDeltaDegrees);
            CancellationTokenSource cancellationSource = new CancellationTokenSource();

            lock (stateLock)
            {
                moveCancellation = cancellationSource;
            }

            try
            {
                SendBlindCommand(command, true, true);

                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    BeginTrackedMoveLocked(logicalDeltaDegrees, logicalTarget);
                }
            }
            catch
            {
                lock (stateLock)
                {
                    isMoving = false;
                    targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                    moveCancellation = null;
                    ResetActiveMoveEstimateLocked();
                }

                cancellationSource.Dispose();
                throw;
            }

            activeMoveTask = Task.Run(() => WaitForMoveCompletion(logicalTarget, logicalDeltaDegrees, cancellationSource.Token), cancellationSource.Token);
        }

        private static void WaitForMoveCompletion(float requestedLogicalTarget, float requestedLogicalDelta, CancellationToken cancellationToken)
        {
            float currentSegmentDelta = requestedLogicalDelta;
            int correctionAttempt = 0;

            try
            {
                while (true)
                {
                    string response = ReceiveResponse(IsMoveCompletionComplete, MoveCompletionTimeout, cancellationToken, true);
                    MoveCompletionResult moveCompletion = ParseMoveCompletionResponse(response);
                    DateTime completedUtc = DateTime.UtcNow;
                    bool requiresCorrection = false;
                    float correctionDelta = 0.0f;
                    float currentLogicalPosition = 0.0f;
                    float correctionError = 0.0f;

                    lock (stateLock)
                    {
                        UpdateEstimatedMotionStateLocked(completedUtc);
                        float completedFirmwareMechanicalPosition = NormalizeAngle(moveCompletion.MechanicalDegrees);
                        float completedDeltaDegrees = completedFirmwareMechanicalPosition - firmwareMechanicalPosition;
                        firmwareMechanicalPosition = completedFirmwareMechanicalPosition;
                        virtualMechanicalPosition = NormalizeAngle(virtualMechanicalPosition + completedDeltaDegrees);
                        currentLogicalPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                        correctionError = AbsoluteAngularDifference(currentLogicalPosition, requestedLogicalTarget);
                        UpdateEstimatedRateFromCompletedMoveLocked(Math.Abs(currentSegmentDelta), completedUtc);

                        if (correctionAttempt < MaximumCorrectionAttempts && correctionError > completionCorrectionThresholdDegrees)
                        {
                            correctionDelta = ComputeCableSafeDeltaLocked(ConvertLogicalToVirtualMechanicalTargetLocked(requestedLogicalTarget));

                            if (Math.Abs(correctionDelta) > MinimumMoveDegrees)
                            {
                                requiresCorrection = true;
                            }
                        }

                        if (!requiresCorrection)
                        {
                            targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                            isMoving = false;
                            ResetActiveMoveEstimateLocked();
                        }
                    }

                    PersistVirtualMechanicalPosition(VirtualMechanicalPosition);

                    if (requiresCorrection)
                    {
                        correctionAttempt++;
                        currentSegmentDelta = correctionDelta;
                        LogMessage(
                            "MoveComplete",
                            $"Move finished with residual error {correctionError.ToString(CultureInfo.InvariantCulture)} degrees; issuing correction {correctionAttempt} of {MaximumCorrectionAttempts} with move {correctionDelta.ToString(CultureInfo.InvariantCulture)} degrees toward target {requestedLogicalTarget.ToString(CultureInfo.InvariantCulture)}.");
                        SendBlindCommand(CreateMoveCommand(correctionDelta), true, true);

                        lock (stateLock)
                        {
                            BeginTrackedMoveLocked(correctionDelta, requestedLogicalTarget);
                        }

                        continue;
                    }

                    LogMessage(
                        "MoveComplete",
                        $"Move finished. Rotated {moveCompletion.ReportedDeltaDegrees} degrees, firmware mechanical {moveCompletion.MechanicalDegrees}, virtual mechanical {VirtualMechanicalPosition}, logical target requested {requestedLogicalTarget}.");
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("MoveComplete", "Move wait cancelled.");
            }
            catch (Exception ex)
            {
                lock (stateLock)
                {
                    UpdateEstimatedMotionStateLocked();
                    isMoving = false;
                    targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                    ResetActiveMoveEstimateLocked();
                }

                LogMessage("MoveComplete", $"Move completion failed: {ex.Message}");
            }
            finally
            {
                lock (stateLock)
                {
                    if (moveCancellation != null && moveCancellation.Token == cancellationToken)
                    {
                        moveCancellation.Dispose();
                        moveCancellation = null;
                    }

                    if (activeMoveTask != null && activeMoveTask.IsCompleted)
                    {
                        activeMoveTask = null;
                    }
                }
            }
        }

        private static void StopActiveMoveInternal(bool sendStopCommand)
        {
            CancellationTokenSource cancellationSource;
            Task moveTask;

            lock (stateLock)
            {
                cancellationSource = moveCancellation;
                moveTask = activeMoveTask;

                if (!isMoving && cancellationSource == null)
                {
                    return;
                }

                UpdateEstimatedMotionStateLocked();
                isMoving = false;
                targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                ResetActiveMoveEstimateLocked();
            }

            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
            }

            if (sendStopCommand && ConnectionActive)
            {
                try
                {
                    SendBlindCommand(StopCommand, false);
                }
                catch (Exception ex)
                {
                    LogMessage("Halt", $"Stop command failed: {ex.Message}");
                }
            }

            lock (serialLock)
            {
                if (serialPort != null && serialPort.Connected)
                {
                    try
                    {
                        serialPort.ClearBuffers();
                    }
                    catch
                    {
                    }
                }
            }

            if (moveTask != null)
            {
                try
                {
                    moveTask.Wait(1000);
                }
                catch
                {
                }
            }

            lock (stateLock)
            {
                if (moveCancellation == cancellationSource)
                {
                    moveCancellation = null;
                }

                activeMoveTask = null;
            }

            if (cancellationSource != null)
            {
                cancellationSource.Dispose();
            }
        }

        private static string CreateMoveCommand(float logicalDeltaDegrees)
        {
            double hardwareDeltaDegrees = Reverse ? -logicalDeltaDegrees : logicalDeltaDegrees;
            int commandValue = (int)Math.Round(hardwareDeltaDegrees * MoveCommandScale, MidpointRounding.AwayFromZero);

            if (commandValue == 0)
            {
                throw new InvalidValueException("Move", logicalDeltaDegrees.ToString(CultureInfo.InvariantCulture), $"at least {MinimumMoveDegrees.ToString(CultureInfo.InvariantCulture)} degrees");
            }

            LogMessage("CreateMoveCommand", $"Logical delta {logicalDeltaDegrees} -> hardware delta {hardwareDeltaDegrees} -> command {commandValue}");
            return commandValue.ToString(CultureInfo.InvariantCulture);
        }

        private static string SendCommandAndReceive(string command, Func<string, bool> completionPredicate, TimeSpan timeout, CancellationToken cancellationToken)
        {
            SendBlindCommand(command, true);
            return ReceiveResponse(completionPredicate, timeout, cancellationToken);
        }

        private static string ReceiveResponse(Func<string, bool> completionPredicate, TimeSpan timeout, CancellationToken cancellationToken, bool extractMoveCompletionResponse)
        {
            StringBuilder responseBuilder = new StringBuilder();
            DateTime deadline = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow <= deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    char nextCharacter;

                    lock (serialLock)
                    {
                        if (serialPort == null || !serialPort.Connected)
                        {
                            throw new NotConnectedException("The WandererRotator serial connection is not open.");
                        }

                        nextCharacter = (char)serialPort.ReceiveByte();
                    }

                    responseBuilder.Append(nextCharacter);
                    string response = responseBuilder.ToString();
                    string normalizedResponse = NormalizeResponse(response);

                    if (string.Equals(normalizedResponse, LowVoltageResponse, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DriverException("The rotator reported low input voltage and refused to move.");
                    }

                    if (completionPredicate(normalizedResponse))
                    {
                        string completedResponse = extractMoveCompletionResponse ? ExtractMoveCompletionResponse(normalizedResponse) : normalizedResponse;
                        LogMessage("Serial Rx", completedResponse);
                        return completedResponse;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (NotConnectedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        throw new DriverException($"Timed out waiting for a response from the rotator. Partial response: '{responseBuilder}'", ex);
                    }
                }
            }

            throw new DriverException($"Timed out waiting for a response from the rotator. Partial response: '{responseBuilder}'");
        }

        private static void SendBlindCommand(string command, bool clearBuffersBeforeWrite)
        {
            SendBlindCommand(command, clearBuffersBeforeWrite, false);
        }

        private static void SendBlindCommand(string command, bool clearBuffersBeforeWrite, bool suppressLogging)
        {
            lock (serialLock)
            {
                if (serialPort == null || !serialPort.Connected)
                {
                    throw new NotConnectedException("The WandererRotator serial connection is not open.");
                }

                if (clearBuffersBeforeWrite)
                {
                    serialPort.ClearBuffers();
                }

                if (!suppressLogging)
                {
                    LogMessage("Serial Tx", command);
                }

                serialPort.Transmit(command);
            }
        }

        private static string ReceiveResponse(Func<string, bool> completionPredicate, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ReceiveResponse(completionPredicate, timeout, cancellationToken, false);
        }

        private static bool IsHandshakeComplete(string response)
        {
            return HandshakeRegex.IsMatch(response);
        }

        private static bool IsMoveCompletionComplete(string response)
        {
            return MoveCompletionRegex.Match(response).Success;
        }

        private static HandshakeResult ParseHandshakeResponse(string response)
        {
            Match match = HandshakeRegex.Match(response);
            if (!match.Success)
            {
                throw new DriverException($"Unexpected handshake response: {response}");
            }

            return new HandshakeResult
            {
                Model = match.Groups["model"].Value,
                FirmwareVersion = match.Groups["firmware"].Value,
                MechanicalDegrees = ParseScaledMechanicalDegrees(match.Groups["mechanical"].Value),
                BacklashDegrees = ParseFloat(match.Groups["backlash"].Value, backlashDefault),
                Reverse = match.Groups["reverse"].Value == "1"
            };
        }

        private static MoveCompletionResult ParseMoveCompletionResponse(string response)
        {
            Match match = MoveCompletionRegex.Match(response);
            if (!match.Success)
            {
                throw new DriverException($"Unexpected move completion response: {response}");
            }

            return new MoveCompletionResult
            {
                ReportedDeltaDegrees = ParseFloat(match.Groups["delta"].Value, "0"),
                MechanicalDegrees = ParseScaledMechanicalDegrees(match.Groups["mechanical"].Value)
            };
        }

        private static float ParseScaledMechanicalDegrees(string rawValue)
        {
            int scaledValue = int.Parse(rawValue, CultureInfo.InvariantCulture);
            return NormalizeAngle(scaledValue / 1000.0);
        }

        private static float ParseFloat(string rawValue, string fallback)
        {
            float parsedValue;
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return float.Parse(fallback, CultureInfo.InvariantCulture);
        }

        private static void UpdateEstimatedMotionStateLocked()
        {
            UpdateEstimatedMotionStateLocked(DateTime.UtcNow);
        }

        private static void UpdateEstimatedMotionStateLocked(DateTime sampleTimeUtc)
        {
            if (!isMoving)
            {
                return;
            }

            double moveDistance = Math.Abs(activeMoveSignedDegrees);
            if (moveDistance < MinimumMoveDegrees)
            {
                virtualMechanicalPosition = activeMoveTargetVirtualMechanicalPosition;
                firmwareMechanicalPosition = activeMoveTargetFirmwareMechanicalPosition;
                return;
            }

            float motionRate = Math.Max(estimatedDegreesPerSecond, MinimumStepDegrees);
            double estimatedDurationSeconds = moveDistance / motionRate;
            double elapsedSeconds = Math.Max(0.0, (sampleTimeUtc - activeMoveStartUtc).TotalSeconds);
            double progress = estimatedDurationSeconds <= 0.0 ? 1.0 : elapsedSeconds / estimatedDurationSeconds;
            progress = Math.Max(0.0, Math.Min(progress, 1.0));

            virtualMechanicalPosition = NormalizeAngle(activeMoveStartVirtualMechanicalPosition + (activeMoveSignedDegrees * progress));
            firmwareMechanicalPosition = NormalizeAngle(activeMoveStartFirmwareMechanicalPosition + (activeMoveSignedDegrees * progress));

            if (progress >= 1.0 && waitingForCompletionFrame && elapsedSeconds >= estimatedDurationSeconds + MoveCompletionGracePeriod.TotalSeconds)
            {
                isMoving = false;
                waitingForCompletionFrame = false;
                virtualMechanicalPosition = activeMoveTargetVirtualMechanicalPosition;
                firmwareMechanicalPosition = activeMoveTargetFirmwareMechanicalPosition;
                targetPosition = NormalizeAngle(virtualMechanicalPosition + syncOffset);
                PersistVirtualMechanicalPosition(virtualMechanicalPosition);
                LogMessage("MoveComplete", $"No completion frame received after the estimated move duration; finalizing move from local state. Firmware mechanical {firmwareMechanicalPosition.ToString(CultureInfo.InvariantCulture)}, virtual mechanical {virtualMechanicalPosition.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        private static void UpdateEstimatedRateFromCompletedMoveLocked(float completedMoveDegrees, DateTime completedUtc)
        {
            if (completedMoveDegrees < MinimumMoveDegrees)
            {
                return;
            }

            double elapsedSeconds = Math.Max(0.001, (completedUtc - activeMoveStartUtc).TotalSeconds);
            float observedDegreesPerSecond = (float)(completedMoveDegrees / elapsedSeconds);

            if (float.IsNaN(observedDegreesPerSecond) || float.IsInfinity(observedDegreesPerSecond) || observedDegreesPerSecond <= 0.0f)
            {
                return;
            }

            estimatedDegreesPerSecond = (estimatedDegreesPerSecond * 0.7f) + (observedDegreesPerSecond * 0.3f);
            LogMessage("MoveComplete", $"Observed motion rate {observedDegreesPerSecond.ToString(CultureInfo.InvariantCulture)} deg/s, estimate now {estimatedDegreesPerSecond.ToString(CultureInfo.InvariantCulture)} deg/s.");
        }

        private static void ResetActiveMoveEstimateLocked()
        {
            activeMoveSignedDegrees = 0.0f;
            activeMoveStartVirtualMechanicalPosition = virtualMechanicalPosition;
            activeMoveStartFirmwareMechanicalPosition = firmwareMechanicalPosition;
            activeMoveTargetVirtualMechanicalPosition = virtualMechanicalPosition;
            activeMoveTargetFirmwareMechanicalPosition = firmwareMechanicalPosition;
            activeMoveStartUtc = DateTime.UtcNow;
            waitingForCompletionFrame = false;
        }

        private static void BeginTrackedMoveLocked(float logicalDeltaDegrees, float logicalTarget)
        {
            targetPosition = logicalTarget;
            isMoving = true;
            activeMoveStartVirtualMechanicalPosition = virtualMechanicalPosition;
            activeMoveStartFirmwareMechanicalPosition = firmwareMechanicalPosition;
            activeMoveSignedDegrees = logicalDeltaDegrees;
            activeMoveTargetVirtualMechanicalPosition = NormalizeAngle(virtualMechanicalPosition + logicalDeltaDegrees);
            activeMoveTargetFirmwareMechanicalPosition = NormalizeAngle(firmwareMechanicalPosition + logicalDeltaDegrees);
            activeMoveStartUtc = DateTime.UtcNow;
            waitingForCompletionFrame = true;
        }

        private static float ConvertLogicalToVirtualMechanicalTargetLocked(float logicalTarget)
        {
            return NormalizeAngle(logicalTarget - syncOffset);
        }

        private static float ComputeCableSafeDeltaLocked(float targetVirtualMechanicalPosition)
        {
            float virtualToFirmwareOffset = virtualMechanicalPosition - firmwareMechanicalPosition;
            float targetFirmwareMechanicalPosition = NormalizeAngle(targetVirtualMechanicalPosition - virtualToFirmwareOffset);
            float cableSafeDelta = targetFirmwareMechanicalPosition - firmwareMechanicalPosition;

            LogMessage(
                "CableSafeDelta",
                $"Current firmware mechanical {firmwareMechanicalPosition.ToString(CultureInfo.InvariantCulture)}, current virtual mechanical {virtualMechanicalPosition.ToString(CultureInfo.InvariantCulture)}, target virtual mechanical {targetVirtualMechanicalPosition.ToString(CultureInfo.InvariantCulture)}, target firmware mechanical {targetFirmwareMechanicalPosition.ToString(CultureInfo.InvariantCulture)}, cable-safe delta {cableSafeDelta.ToString(CultureInfo.InvariantCulture)}.");

            return cableSafeDelta;
        }

        private static string NormalizeResponse(string response)
        {
            return response.Trim('\0').TrimEnd('\r', '\n');
        }

        private static string ExtractMoveCompletionResponse(string normalizedResponse)
        {
            MatchCollection matches = MoveCompletionRegex.Matches(normalizedResponse);
            if (matches.Count > 0)
            {
                return matches[matches.Count - 1].Value;
            }

            return normalizedResponse;
        }

        private static float NormalizeAngle(double angle)
        {
            double normalizedAngle = angle % 360.0;
            if (normalizedAngle < 0.0)
            {
                normalizedAngle += 360.0;
            }

            return (float)normalizedAngle;
        }

        private static float AbsoluteAngularDifference(float firstAngle, float secondAngle)
        {
            float delta = NormalizeAngle(secondAngle - firstAngle);
            return delta > 180.0f ? 360.0f - delta : delta;
        }

        private sealed class HandshakeResult
        {
            internal string Model { get; set; }

            internal string FirmwareVersion { get; set; }

            internal float MechanicalDegrees { get; set; }

            internal float BacklashDegrees { get; set; }

            internal bool Reverse { get; set; }
        }

        private sealed class MoveCompletionResult
        {
            internal float ReportedDeltaDegrees { get; set; }

            internal float MechanicalDegrees { get; set; }
        }

        #endregion
    }
}

