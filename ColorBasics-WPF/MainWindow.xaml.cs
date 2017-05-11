//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect.Face;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.Kinect.SmartRoom
{
    using Samples.Kinect.ColorBasics.Properties;


    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string _StatusText;
        private WriteableBitmap _ColorBitmap;
        
        static volatile bool _Processing = false;

        private KinectSensor _KinectSensor;
        private ColorFrameReader _ColorFrameReader;
        private BodyFrameReader _BodyReader;
        private FaceFrameReader _FaceReader;
        private FaceFrameSource _FaceSource;

        private FacialRecognizer _FacialRecognizer;

        private UserEventController _UserEventController;

        private IList<Body> _Bodies;

        private double _LeftEyeCoordX;
        private double _LeftEyeCoordY;

        private Guid _SessionId;
        private string _Folder;

        public MainWindow()
        {
            InitializeComponent();

            _SessionId = Guid.NewGuid();
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _Folder = Path.Combine(programData, "Kinect", _SessionId.ToString());

            _KinectSensor = KinectSensor.GetDefault();
            _FacialRecognizer = new FacialRecognizer();

            _UserEventController = new UserEventController();
            UserRecognized += _UserEventController.OnUserRecognized;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = _KinectSensor.ColorFrameSource.CreateFrameDescription(
                ColorImageFormat.Bgra);

            // create the bitmap to display
            _ColorBitmap = new WriteableBitmap(colorFrameDescription.Width, 
                colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set IsAvailableChanged event notifier
            _KinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // open the sensor
            _KinectSensor.Open();

            // set the status text
            StatusText = _KinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            // use the window object as the view model in this simple example
            DataContext = this;

            // initialize the components (controls) of the window
            _Bodies = new Body[_KinectSensor.BodyFrameSource.BodyCount];

            _BodyReader = _KinectSensor.BodyFrameSource.OpenReader();
            _BodyReader.FrameArrived += BodyReader_FrameArrived;

            _FaceSource = new FaceFrameSource(_KinectSensor, 0, FaceFrameFeatures.BoundingBoxInColorSpace |
                                                              FaceFrameFeatures.FaceEngagement |
                                                              FaceFrameFeatures.Glasses |
                                                              FaceFrameFeatures.Happy |
                                                              FaceFrameFeatures.LeftEyeClosed |
                                                              FaceFrameFeatures.MouthOpen |
                                                              FaceFrameFeatures.PointsInColorSpace |
                                                              FaceFrameFeatures.RightEyeClosed);
            _FaceReader = _FaceSource.OpenReader();
            _FaceReader.FrameArrived += FaceReader_FrameArrived;

            _ColorFrameReader = _KinectSensor.ColorFrameSource.OpenReader();
            _ColorFrameReader.FrameArrived += Reader_ColorFrameArrived;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event UserRecognizedEventHandler UserRecognized;
        protected void OnUserRecognized(string name, DateTime ts)
        {
            UserRecognized?.Invoke(this, new UserRecognizedEventArgs(name, ts));
        }

        public ImageSource ImageSource => _ColorBitmap;

        public double LeftEyeCoordX
        {
            get
            {
                return _LeftEyeCoordX;
            }
            set
            {
                _LeftEyeCoordX = value;
                if (PropertyChanged != null)
                {
                    OnPropertyChanged("LeftEyeCoordX");
                }
            }
        }

        public double LeftEyeCoordY
        {
            get
            {
                return _LeftEyeCoordY;
            }
            set
            {
                _LeftEyeCoordY = value;
                if (PropertyChanged != null)
                {
                    OnPropertyChanged("LeftEyeCoordY");
                }
            }
        }

        public string StatusText
        {
            get
            {
                return _StatusText;
            }

            set
            {
                if (_StatusText != value)
                {
                    _StatusText = value;

                    // notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusText"));
                }
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_ColorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                _ColorFrameReader.Dispose();
                _ColorFrameReader = null;

                _FaceReader.Dispose();
                _FaceReader = null;
            }

            if (_KinectSensor != null)
            {
                _KinectSensor.Close();
                _KinectSensor = null;
            }

            if (Settings.Default.DeleteSavedPhotos && Directory.Exists(_Folder))
            {
                Directory.Delete(_Folder, true);    
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        _ColorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == _ColorBitmap.PixelWidth) && (colorFrameDescription.Height == _ColorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                _ColorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            _ColorBitmap.AddDirtyRect(new Int32Rect(0, 0, _ColorBitmap.PixelWidth, _ColorBitmap.PixelHeight));
                        }

                        _ColorBitmap.Unlock();
                    }
                }
            }
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_Bodies);

                    Body body = _Bodies.FirstOrDefault(b => b.IsTracked);

                    if (!_FaceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            // 4) Assign a tracking ID to the face source
                            _FaceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                // 4) Get the face frame result
                FaceFrameResult result = frame?.FaceFrameResult;
                    
                if (result != null && !_Processing)
                {
                    _Processing = true;
                    // Get the face points, mapped in the color space.
                        

                    var eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                    LeftEyeCoordX = eyeLeft.X;
                    LeftEyeCoordY = eyeLeft.Y;

                    var faceCoor = result.FaceBoundingBoxInColorSpace;
                    
                    using (ColorFrame colorFrame = frame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            Debug.WriteLine("Got color frame");
                            FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                            var wrBitmap = new WriteableBitmap(colorFrameDescription.Width,
                                colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                            {
                                Debug.WriteLine("Locked image");
                                wrBitmap.Lock();

                                if ((colorFrameDescription.Width == wrBitmap.PixelWidth) &&
                                    (colorFrameDescription.Height == wrBitmap.PixelHeight))
                                {
                                    Debug.WriteLine("Send image to save");
                                    var array =
                                        new byte[(uint) (colorFrameDescription.Width * colorFrameDescription.Height * 4)
                                            ];
                                    colorFrame.CopyConvertedFrameDataToArray(array, ColorImageFormat.Bgra);
                                    
                                    DateTime frameTS = DateTime.Now;

                                    Tuple<byte[], FrameDescription, RectI, DateTime> tuple = Tuple.Create(array,
                                        colorFrameDescription, faceCoor, frameTS);

                                    BackgroundWorker worker = new BackgroundWorker();
                                    worker.DoWork += RecognizeImage;
                                    worker.RunWorkerAsync(tuple);
                                }
                                else
                                {
                                    _Processing = false;
                                }

                                wrBitmap.Unlock();
                            }
                        }
                        else
                        {
                            _Processing = false;
                        }
                    }
                }
            }
        }

        private void RecognizeImage(object sender, DoWorkEventArgs e)
        {
            var tuple = (Tuple<byte[], FrameDescription, RectI, DateTime>)e.Argument;

            byte[] array = tuple.Item1;
            var desc = tuple.Item2;
            var face = tuple.Item3;
            var frameTS = tuple.Item4;
            var faceWidth = face.Right - face.Left;
            var faceHeight = face.Bottom - face.Top;

            if (faceWidth != 0 && faceHeight != 0)
            {
                string name = Guid.NewGuid().ToString();
                if (!Directory.Exists(_Folder))
                    Directory.CreateDirectory(_Folder);
                string path = Path.Combine(_Folder, name + ".jpeg");

                Bitmap bmp = new Bitmap(desc.Width, desc.Height);
                ImageHelper.TransferPixelsToBitmapObject(bmp, array);
                //bmp = ImageHelper.CropAtRect(bmp, new Rectangle(face.Left, face.Top, faceWidth, faceHeight));
                bmp.Save(path, ImageFormat.Jpeg);
                path = @"C:\ProgramData\Kinect\7162c1b0-f846-463c-b4e0-0b6b52fd14a9.jpeg";
                Debug.WriteLine("Try to recognize");
                IEnumerable<string> recognitionResult = _FacialRecognizer.Recognize(path).ToList();
                //IEnumerable<string> recognitionResult = new List<string>();
                foreach (var item in recognitionResult)
                {
                    OnUserRecognized(item, frameTS);
                }

                Debug.WriteLine(string.Join(", ", recognitionResult));
                bmp = null;
                GC.Collect();
            }
            Debug.WriteLine("Recognized " + DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.CurrentUICulture.DateTimeFormat));
            _Processing = false;
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            StatusText = _KinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
