using System;
using System.Drawing;
using System.IO;
using System.Threading;

namespace USG_Access_Service
{
    internal class CommandHandler
    {
        public const int MAX_COMMAND_LENGTH = 20;
        private const double _FRAMERATE = 5.6;

        private readonly ConnectionHandler _connection;
        private long _nextShotTimeTicks;
        private readonly AutomationHandler _automation;

        public CommandHandler(ConnectionHandler connection)
        {
            _connection = connection;
            _automation = new AutomationHandler();
        }

        public void HandleCommand(string command)
        {
                try
                {
                    switch (command)
                    {
                        case Command.GET_PICTURE:
                            SendScreenShot();
                            break;
                        case Command.FREEZE:
                            Console.WriteLine("Pressing freeze button");
                            _automation.Freeze();
                            _connection.SendString("Freeze invoked");
                            break;
                        case Command.GAIN_UP:
                            Console.WriteLine("Pressing gain up button");
                            _automation.GainUp();
                            _connection.SendString(_automation.GainValue);
                            break;
                        case Command.GAIN_DOWN:
                            Console.WriteLine("Pressing gain down button");
                            _automation.GainDown();
                            _connection.SendString(_automation.GainValue);
                            break;
                        case Command.AREA_UP:
                            Console.WriteLine("Pressing area up button");
                            _automation.AreaUp();
                            _connection.SendString(_automation.ImagingRangeValue);
                            break;
                        case Command.AREA_DOWN:
                            Console.WriteLine("Pressing area down button");
                            _automation.AreaDown();
                            _connection.SendString(_automation.ImagingRangeValue);
                            break;
                        case Command.HIDE:
                            Console.WriteLine("Pressing hide button");
                            _automation.HideConsole();
                            _connection.SendString(_automation.IsConsoleVisible?"Visible":"Hidden");
                            break;
                    //case Command.SAVE:
                    //    Console.WriteLine("Pressing save button");
                    //    InvokeElement("Save_Btn");
                    //    _connection.SendString("Save pressed");
                    //    break;

                    // Palette change
                    case Command.PALETTE_LINEAR:
                            Console.WriteLine("Changing palette to " + command);
                            _automation.PaletteChange("8-bit linear grayscale");
                            _connection.SendString(_automation.PaletteValue);
                            break;
                        case Command.PALETTE_LOG_1_5:
                            Console.WriteLine("Changing palette to " + command);
                            _automation.PaletteChange("8-bit log 1.5f grayscale");
                            _connection.SendString(_automation.PaletteValue);
                            break;
                        case Command.PALETTE_LOG_1_75:
                            Console.WriteLine("Changing palette to " + command);
                            _automation.PaletteChange("8-bit log 1.75f grayscale");
                            _connection.SendString(_automation.PaletteValue);
                            break;
                        case Command.PALETTE_LOG_2_0:
                            Console.WriteLine("Changing palette to " + command);
                            _automation.PaletteChange("8-bit log 2.0f grayscale");
                            _connection.SendString(_automation.PaletteValue);
                            break;
                        case Command.PALETTE_LOG_3_0:
                            Console.WriteLine("Changing palette to " + command);
                            _automation.PaletteChange("8-bit log 3.0f grayscale");
                            _connection.SendString(_automation.PaletteValue);
                            break;

                        // TX Signal change
                        case Command.SIGNAL_SINE_1_25:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("Sine 1 cycle, 25 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_SINE_4_25:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("Sine 4 cycles, 25 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_SINE_6_25:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("Sine 6 cycles, 25 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_SINE_16_25:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("Sine 16 cycles, 25 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_13_BIT_20:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("13-bit Barker, 20 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_13_BIT_35:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("13-bit Barker, 35 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;
                        case Command.SIGNAL_16_BIT_CHIRP:
                            Console.WriteLine("Changing TX Signal to " + command);
                            _automation.SignalChange("16-bit Chirp, 15-25 MHz");
                            _connection.SendString(_automation.TxSignalValue);
                            break;

                        case Command.GET_GAIN:
                            _connection.SendString(_automation.GainValue);
                            Console.WriteLine("Sent gain value");
                            break;
                        case Command.GET_TX_FREQUENCY:
                            _connection.SendString(_automation.TxFrequencyValue);
                            Console.WriteLine("Sent TX frequency");
                            break;
                        case Command.GET_TX_TYPE:
                            _connection.SendString(_automation.TxTypeValue);
                            Console.WriteLine("Sent TX type");
                            break;
                        case Command.GET_AREA:
                            _connection.SendString(_automation.ImagingRangeValue);
                            Console.WriteLine("Sent imaging range");
                            break;
                        case Command.GET_FPS:
                            _connection.SendString(_automation.FramerateValue);
                            Console.WriteLine("Sent FPS");
                            break;
                        default:
                            Console.WriteLine("Error: Unrecognized request command: " + command);
                            break;
                    }
                }
                catch (ApplicationException e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    if (_connection.IsClientConnected)
                    {
                        _connection.SendError(e.Message);
                    }
                    Console.WriteLine("Restarting automation...");
                    _automation.Initialize();
                }
        }

        public void SendScreenShot()
        {
            var curTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (curTimeMs < _nextShotTimeTicks)
            {
                Thread.Sleep((int)(_nextShotTimeTicks - curTimeMs));
            }
            _nextShotTimeTicks = curTimeMs + (int)(1000 / _FRAMERATE);

            Bitmap bmp = TakeScreenshot();
            MemoryStream memoryStream = new MemoryStream();

            // Save to memory using the JPEG format
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            // Read to end
            byte[] bmpBytes = memoryStream.ToArray();
            bmp.Dispose();
            memoryStream.Close();

            _connection.SendByteArray(bmpBytes);
        }

        private Bitmap TakeScreenshot()
        {
            // GoogleGlass display: 640x360 @ 24 bits, 16:9 aspect ratio.
            Point upperLeftPoint;
            Point lowerRightPoint;
            if (_automation.IsConsoleVisible)
            {
                upperLeftPoint = new Point(337, 162);
                lowerRightPoint = new Point(1022, 461);
            }
            else
            {
                upperLeftPoint = new Point(342, 193);
                lowerRightPoint = new Point(1256, 592);
            }
            var pictureSize = new Size(lowerRightPoint.X - upperLeftPoint.X + 1, lowerRightPoint.Y - upperLeftPoint.Y + 1);

            Bitmap screenshotBmp = new Bitmap(pictureSize.Width, pictureSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics screenshotGraphics = Graphics.FromImage(screenshotBmp);

            screenshotGraphics.CopyFromScreen(
                sourceX: upperLeftPoint.X,
                sourceY: upperLeftPoint.Y,
                destinationX: 0,
                destinationY: 0,
                blockRegionSize: pictureSize,
                copyPixelOperation: CopyPixelOperation.SourceCopy);

            screenshotGraphics.Dispose();

            return screenshotBmp;
        }
    }
}
