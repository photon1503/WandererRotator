
Serial Protocol for WandererRotator Mini/Mini V2-20240226 

1

Automatic Zoom
Serial Protocol for WandererRotator Mini/Mini V2-20240226 
This protocol is ONLY valid for firmware version newer than 20240226 ! 
 
Baud rate: 19200 
Data bits: 8 
Parity: None 
Stop bits: 1 
 
Handshake command: Once the rotator receives the handshake command of 1500001, 
it will send a feedback message to the COM port. The feedback message for handshake has 
the following format:   
WandererRotatorMiniAxxxxxxxxAyyyyAzzAöA 
The red part is the firmware version. The yyyy part is the mechanical angle multiplied by 
1000.  The  zz  part  is  the  backlash.  The  ö  part  is  the  reverse  setting,  0  represents 
normal direction and 1 represents reversed direction. 
 
Action Command Example 
Rotate counterclockwise for angle x 
with reverse setting 0 1142*x 114200 for rotating 100° 
counterclockwise 
Rotate clockwise for angle x with 
reverse setting 0 -1142*x -114200 for rotating 100° 
clockwise 
Set the current mechanical position as 
zero 1500002  
Set the backlash as x 10*x+1600000 1600005 for backlash 0.5° 
Set the rotation direction as normal 1700000  
Set the rotation direction as reversed 1700001  
Stop during rotation stop  
 
After a rotation command is sent and the rotation is completed, the rotator will send a 
feedback message to the COM port. The feedback has the following format: 
xx.xxAyyyyyA 
The xx part is the angle rotated and the yy part is the mechanical angle multiplied by 
1000. 
For example, if a handshake command “1500001” is sent and you get the feedback of 
WandererRotatorMiniA20240226A0A0.5A0A.  It  means  that  the  mechanical  angle  is  0,  the 
backlash is 0.5°  and the rotation direction is normal. Then after sending a rotation command 
of “11420” you will get the feedback of “10.00A10000A”. Again, send a rotation command of 
“50000”, this time you will get the feedback of “43.78A53782A”.  The  last  digit  of  the 
accumulated angle may be affected by rounding error, which can be ignored. 
In  addition,  for  WandererRotator  Mini  V2,  if  the  chip  detects  that  the  input  voltage  is 
under 11V, then it will not move and send an “NP” message to the COM port. 
 
 