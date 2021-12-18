# DocToIdoit

> ## Description
Import ordered products from a delivery note directly to **[i-doit](https://www.i-doit.org)**.

You only have to define your products once in the appsettings.json file.

Everthing after will be autonomous. With OCR, the PDF document will be processed to searchable text.
 
Therefore DocToIdoit will filter out all products which are set up in the appsettings.json and import them with order date, serial number and delivery note number to i-doit.

> ## IronOCR license
DocToIdoit uses [IronOCR](https://ironsoftware.com/csharp/ocr) as ocr engine. If you want to use it in a production environment without a debugger, you need a license for it.

> ## Delivery note document layout
### Make sure that your documents meet this requirements to be processed correctly.
- Only one date has to be on the document (Regex for date detection can be modified in the appsettings.json)
- Some kind of a delivery note number has to be on the document (Regex for delivery note detection can be modified in the appsettings.json)
- The ordered products on that document has to be in this layout:
  - The serial numbers have to begin in the next line after the product name
  - Before the serial numbers begin, there has to be an idicator e.g. "S/N:" or "Serials:", but only at the beginning of the line!
- Example:<br> 
  `` Dell Inspiron 15 3505`` <br>
  `` S/N: EG3439898, EG34898478`` <br>
  `` Dell Inspiron 14 7400 Core i7 16GB RAM`` <br>
  `` S/N: EF456898, EF45558478``


> ## Requirements
### Operating system
- Linux Distribution e.g., Debian/Ubuntu
- Packages: dotnet-runtime-6.0, libgdiplus, libc6-dev
### PDF
- PDFs should have least a resolution of 300 dpi
- PDFs should not be compressed to heavily, the better the quality of the PDF and source document, the more reliable the ocr result.
> ## Installation
 ###  DocToIdoit
 - Setup the requirements
 - Copy the compiled files to a folder on the server e.g. /usr/local/bin/DocToIdoit
 - Create a new user which will be used to run DocToIdoit (Do NOT use root as user)
 - Edit the appsettings.json as your needs
 ###  Documents folder
 - Make sure that your folder for scanned documents has the right permissions, read and write permission for the user which is used for DocToIdoit
 - Create a SMB share or FTP share for the scan folder which is set in the appsettings.json
 - Make sure that new files which are written by SMB or FTP are locked during write/send, otherwise it could happen that DocToIdoit tries to process the new file before write process is finished
 - DocToIdoit will process new files every 30 sec. automatically in that folder
 ###  Usage
 - Start the application with “dotnet DocToIdoit.dll”
 - Recommended: Create a systemd service to start it automatically (a sample file is included in the repository)
