.\.paket\paket.exe install

cd paket-files\devcrafting\DDay.iCal\
nuget install DDay.iCal\packages.config -outputdirectory packages -source C:\Users\Administrator\.nuget\packages\
msbuild DDay.iCal.sln

cd ../../..