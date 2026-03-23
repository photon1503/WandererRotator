# WandererRotator ASCOM Driver

Windows ASCOM local-server rotator driver for WandererRotator hardware.

This repository contains an ASCOM COM driver that talks to the rotator over a serial port and exposes it through the ASCOM `IRotatorV4` interface.

## Status

The driver currently supports:

- Serial connection and handshake over the documented protocol
- Multi-client local-server usage through ASCOM COM
- Relative moves and absolute moves derived from the firmware-reported current angle
- Halt / stop during motion
- Reverse setting support
- Backlash setting support
- Configurable completion-correction threshold
- Configurable default motion-rate estimate
- Read-only display of the currently learned motion rate
- Set-current-position-to-zero action
- Continuous estimated angle updates while a move is in progress
- Parsing of the end-of-move completion frame returned by the device
- Automatic post-move correction retries when the reported final angle is still outside the configured tolerance

The firmware reports its current mechanical angle, while movement is expressed as signed left/right rotation amounts. The driver therefore translates ASCOM requests into protocol move amounts without choosing a different path on the client's behalf.


## Setup Dialog

![alt text](image.png)

The setup dialog currently exposes:

- COM port selection
- Trace logging enable/disable
- Reverse rotation axis
- Backlash value
- Completion-correction threshold
- Default motion-rate estimate in deg/sec
- Measured motion rate in deg/sec (read only)
- Set current position to zero

Behavior:

- Reverse, backlash, completion-correction threshold, and default motion rate are persisted in the ASCOM Profile store
- If the device is already connected, reverse and backlash are applied immediately
- Changing the default motion rate also resets the current internal estimate to that configured value
- Set-to-zero is a live action and requires an active connection

## Motion Model

The firmware is aware of its current mechanical angle, but the movement commands themselves are still directional left/right-by-x commands. Because of that, the driver does the following:

- Converts `MoveAbsolute()` into a signed relative move based on the client's requested absolute target and the current reported angle
- Tracks the logical position separately from the raw mechanical position
- Estimates angle updates during motion until the final completion frame is received
- Uses the device's move-complete response to correct the raw mechanical position
- If the residual error after a completion frame is greater than the configured threshold, issues up to three automatic correction moves toward the requested target
- Starts the tracked correction move only after the correction command has been sent successfully
- Leaves sync handling to `Sync()` rather than reinterpreting every completed move as a new sync event

This keeps `MechanicalPosition` aligned with the device-reported state while letting `Position` reflect the normal ASCOM logical-position model.

## Registering the Driver

After building, register the local server executable once on the target machine using an elevated command prompt:

```powershell
cd .\WandererRotator\bin\Debug
.\ASCOM.photonWanderer.exe /regserver
```

To unregister:

```powershell
.\ASCOM.photonWanderer.exe /unregserver
```

Do not use `RegAsm` on the local-server executable.
## Logging

ASCOM trace logs are written into the standard ASCOM logs folder under the user's Documents directory.

Typical logs include:

- `ASCOM.photonWanderer.Driver.*`
- `ASCOM.photonWanderer.Hardware.*`
- `ASCOM.photonWanderer.LocalServer.*`

The hardware log is the most useful place to debug protocol framing, handshake parsing, move completion, and stop behavior.

## Known Limitations

- The wire protocol does not provide a dedicated absolute-move command; absolute moves are translated into signed left/right travel amounts from the current reported angle
- In-flight angle updates are estimated until the device sends its completion frame
- If the device stops slightly short, the driver may send up to three automatic correction moves before declaring the move complete
- Behavior depends on the serial protocol implemented by the device firmware actually in use
- The protocol examples in the vendor documentation do not fully cover all model-name variants returned in the handshake, so the parser has been made tolerant of device-model differences

## Development Notes

- The project is currently an ASCOM COM local server, not an Alpaca device
- The project target is `x86`; keep that unless you intentionally redesign the local-server architecture
- The most important implementation file is [WandererRotator/RotatorDriver/RotatorHardware.cs](WandererRotator/RotatorDriver/RotatorHardware.cs)



