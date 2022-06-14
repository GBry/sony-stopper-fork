# sony-stopper
An application to shut down a SONY android TV (KD-49XF7596) in case of external input having no signal.

It is a Windows tray-only application written in c# which uses the IP Control feature of the TV.

Before you actually start the app, you need to set the IP and the password fo the IP control in the settings.json
You also have to enable the IP control on your TV with a PSK password.


There was a long research path which lead to this rather quick application.\
Some details you can read on stack Overflow:\
https://stackoverflow.com/questions/72534954/how-to-auto-turn-off-a-sony-android-tv-on-no-signal/72557080#72557080


