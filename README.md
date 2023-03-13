# DocToIdoit

> ## Description
Import ordered products from a delivery note directly to **[i-doit](https://www.i-doit.org)**.

You only have to define your products once in the appsettings.json file.
Everthing after will be autonomous. With OCR, the PDF document will be processed to searchable text.
Therefore DocToIdoit will filter out all products which are set up in the appsettings.json and import them with order date, serial number and delivery note number to i-doit.

> ## IronOCR license
DocToIdoit uses [IronOCR](https://ironsoftware.com/csharp/ocr) as ocr engine. If you want to use it in a production environment without a debugger, you need a license for it. There is also a trial license for 30 days.

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

 ### i-doit
- Running i-doit installation (>=1.17)
- [Modified i-doit API Addon](https://community.i-doit.com/post/16590) to use object templates
- IronOCR license

 ### PDF
- PDFs should have least a resolution of 300 dpi
- PDFs should not be compressed to heavily, the better the quality of the PDF and source document, the more reliable the ocr result.

### Tesseract language file
The default ocr language is German, if you want to use other languages you need to set a custom language file in the appsettings.json.
Take a look [here](https://github.com/tesseract-ocr/tessdata)

> ## Installation (Docker)
 - Install docker and docker-compose
 - Get the docker-compose [file](https://github.com/Kleinrotti/DocToIdoit/blob/main/docker/docker-compose.yml)
 - Edit the docker-compose file as your needs, set your volume paths for the input scan folder and your [appsettings.json](https://github.com/Kleinrotti/DocToIdoit/blob/main/src/DocToIdoit/appsettings.json)
 - Replace ``latest`` in ``kleinrotti/doctoidoit:latest`` with a version you want to use
 - Run ``docker-compose -p doctoidoit up -d``

> ## Installation (bare metal on Ubuntu or Debian)

 ###  DocToIdoit
 - Install this packages: ``dotnet-runtime-6.0 apt-utils libgdiplus libc6-dev tessseract-ocr libtesseract-dev``
 - Copy the [compiled files](https://github.com/Kleinrotti/DocToIdoit/releases) to a folder on the server e.g. /usr/local/bin/DocToIdoit
 - Create a new user which will be used to run DocToIdoit (Do NOT use root as user)
 - Edit the appsettings.json as your needs
 - Run ``dotnet DocToIdoit.dll`` with the user you just created
 - (Optional) Use the sample [doctoidoit.service](https://github.com/Kleinrotti/DocToIdoit/blob/main/doctoidoit.service) file to create a systemd service
 
> ##  Documents folder
 - Make sure that your folder for scanned documents has read and write permission
 - Make sure that new files which are written by SMB or FTP are locked during write/send, otherwise it could happen that DocToIdoit tries to process the new file before write process is finished
 - DocToIdoit will process new files every 30 sec. automatically in that folder

> ## Performance
I recommend at least 2 CPU cores and 2 GB RAM. You can calculate around 100MB per processed page.
If you have large PDFs with many pages or if you turn on parallel processing, ensure you have enough RAM and CPU cores.



## appsettings.json Matrix
| Property   |      Description      |  Required |  Default | Type |
|------------|:---------------------:|----------:|---------:|-----:|
| IronOcr.LicenseKey | IronOCR license key | Yes | - | string |
| Watcher.ScanPath |    Listen for new files in this directory   |   Yes | - | string |
| Watcher.ProcessingPath | Files will be moved there while processing |    Yes | - | string |
| Watcher.ErrorScanPath |  Files will be moved there on errors | Yes | - | string |
| Watcher.OcrResultPath |  After ocr finished, a text file will be created there | Yes | - | string |
| Watcher.ProcessAsync |  Process multiple files in parallel. More RAM and CPU needed. | Yes | False | bool |
| Watcher.ScanInterval |  Interval to scan for new files | Yes | 30000 | int |
| Idoit.Server |  URL to your i-doit Server API | Yes | - | string |
| Idoit.ApiKey |  i-doit Addon api key | Yes | - | string |
| Smtp.MailOnError |  Send an email on processing errors | No | False | bool |
| Smtp.Server |  Smtp Server IP | No | - | string |
| Smtp.Port |  Smtp Server Port | No | 25 | int |
| Smtp.Subject |  Subject of the email | No | - | string |
| Smtp.From |  Sender of the email | No | - | string |
| Smtp.To |  Recipient of the email | No | - | string |
| Smtp.Username |  Username for Smtp server | No | - | string |
| Smtp.Password |  Password for Smtp server | No | - | string |
| Smtp.SSL |  Use use SSL for SMTP connection | No | false | bool |
| Ocr.Scale |  Target scaling of the PDF | Yes | 300 | int |
| Ocr.Deskew |  Correct rotation of the PDF | Yes | True | bool |
| Ocr.DeliveryNoteDetectionRegex |  Regex to detect the delivery note number | Yes | (LIEF).\\d* | string |
| Ocr.DateDetectionRegex |  Regex to detect the date | Yes | \\d{2}.\\d{2}.\\d{4} | string |
| Ocr.TicketIdDetectionRegex |  Regex to detect the ticket id | No | (?<=Ticket# )[^.\\s]* | string |
| Ocr.DateFormat |  Format of the dates | Yes | dd.MM.yyyy | string |
| Ocr.SerialDelimiter |  Delimeter between serial numbers | Yes | , | string |
| Ocr.SerialIndicators |  Indicators to detect the lines where serial number are listed | Yes | S/N: | string[] |
| Ocr.SupportedProducts |  Array of supported products (see matrix below) | No | - | object[] |
| Ocr.CustomLanguageFile |  Path to custom tesseract language file | No | - | string |

## appsettings.json SupportedProducts
| Property   |      Description      |  Required |  Default | Type |
|------------|:---------------------:|----------:|---------:|-----:|
| ProductName |  Search string to find the product in the document | Yes | - | string |
| Type |  i-doit object type | Yes | C__OBJTYPE__MONITOR | string |
| IdoitPrefix |  Prefix for the i-doit object, after the prefix a timestamp will be added | Yes | - | string |
| Template |  Template ID from i-doit which should be used | Yes | - | int |
