This program reenables the save-functionality of Dishonored 2 if the save progress won't
stop. Most likely the reason is the Malwarebytes (MB) Anti-Ransomware service which prevents
finishing saving a game. The Dishonored 2 save files are read-only with an active MB-service.
If you change the user rights the problem persists because Dishonored 2 does not necessarily
rewrite save files. Instead new files will get created, which are read-only again.

Generally this program stops the service (and thus the ransomware protection by closing the
program) and starts a user defined program. This doesn't have to be Dishonored2.exe, it can
be any program. If the program is terminated the ransomware protection gets restarted.