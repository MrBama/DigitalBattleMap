@echo off
set /P url=Enter url (e.g. http://localhost:8000): 
IF "%url%" == "" set url=http://localhost:8000
DigitalBattleMapServer.exe --urls %url%