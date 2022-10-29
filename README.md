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
- Linux Distribution e.g., Debian/Ubuntu or Windows
- Packages: dotnet-runtime-6.0, libgdiplus, libc6-dev
### i-doit
- Running i-doit installation (>=1.17)
- i-doit API Addon
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
 - Make sure that your folder for scanned documents has read and write permission for the user which is used for DocToIdoit
 - Create a SMB share or FTP share for the scan folder which is set in the appsettings.json
 - Make sure that new files which are written by SMB or FTP are locked during write/send, otherwise it could happen that DocToIdoit tries to process the new file before write process is finished
 - DocToIdoit will process new files every 30 sec. automatically in that folder
 ###  Usage
 - Start the application with “dotnet DocToIdoit.dll”
 - Recommended: Create a systemd service to start it automatically (a sample file is included in the repository)
 - Scan your documents as PDF to the scan folder via SMB/FTP or any other protocol
 - For each serial number in the document an object will be created in i-doit, if it's added in the appsettings.json under SupportedProducts

## Performance
I recommend 2 CPU cores and 2 GB RAM as minimum. You can calculate around 100MB per processed page.
If you have large PDFs with many pages or if you turn on parallel processing, ensure you have enough RAM.



## appsettings.json Matrix
| Property   |      Description      |  Required |  Default | Type |
|------------|:---------------------:|----------:|---------:|-----:|
| File.Path |  Log file name | Yes | - | string |
| Watcher.ScanPath |    Listen for new files in this directory   |   Yes | - | string |
| Watcher.ProcessingPath | Files will be moved there while processing |    Yes | - | string |
| Watcher.ErrorScanPath |  Files will be moved there on errors | Yes | - | string |
| Watcher.OcrResultPath |  After ocr finished, a text file will be created there | Yes | - | string |
| Watcher.ProcessAsync |  Process multiple files in parallel. More RAM and CPU needed. | Yes | False | bool |
| Idoit.Server |  URL to your i-doit Server API | Yes | - | string |
| Idoit-ApiKey |  i-doit Addon api key | Yes | - | string |
| Smtp.MailOnError |  Send an email on processing errors | No | False | bool |
| Smtp.Server |  Smtp Server IP | No | - | string |
| Smtp.Port |  Smtp Server Port | No | 25 | int |
| Smtp.Subject |  Subject of the email | No | - | string |
| Smtp.From |  Sender of the email | No | - | string |
| Smtp.To |  Recipient of the email | No | - | string |
| Ocr.Scale |  Scaling of the PDF | Yes | 300 | int |
| Ocr.Deskew |  Correct rotation of the PDF | Yes | True | bool |
| Ocr.License |  IronOCR license key | No | - | string |
| Ocr.DeliveryNoteDetectionRegex |  Regex to detect the delivery note number | Yes | (LIEF).\\d* | string |
| Ocr.DateDetectionRegex |  Regex to detect the date | Yes | \\d{2}.\\d{2}.\\d{4} | string |
| Ocr.SerialDelimiter |  Delimeter between serial numbers | Yes | , | string |
| Ocr.SerialIndicators |  Indicators to detect the lines where serial number are listed | Yes | S/N: | string[] |
| Ocr.SupportedProducts |  Array of supported products | No | - | object[] |

## appsettings.json SupportedProducts
| Property   |      Description      |  Required |  Default | Type |
|------------|:---------------------:|----------:|---------:|-----:|
| ProductName |  Search string to find the product in the document | Yes | - | string |
| Type |  i-doit object type | Yes | C__OBJTYPE__MONITOR | string |
| IdoitPrefix |  Prefix for the i-doit object, after the prefix a timestamp will be added | Yes | - | string |
| Template |  Template ID from i-doit which should be used | Yes | - | int |