using System;

#if ANDROID
using Android.Content;
using Android.Hardware;
using Android.Runtime;
#endif

namespace Schedule2._0.Services
{
    public class LightSensorService
#if ANDROID
        : Java.Lang.Object, ISensorEventListener
#endif
    {
        // 阈值定义：15.0f 以下判定为昏暗
        private const float DimLuxThreshold = 15.0f;
        private float _lastReading = 20.0f; // 初始设为明亮，防止误触发

        private static LightSensorService? _instance;
        public static LightSensorService Instance => _instance ??= new LightSensorService();

#if ANDROID
        private SensorManager? _sensorManager;
        private Sensor? _lightSensor;
#endif

        private LightSensorService()
        {
#if ANDROID
            // 获取安卓系统传感器服务
            _sensorManager = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.GetSystemService(Context.SensorService) as SensorManager;
            _lightSensor = _sensorManager?.GetDefaultSensor(SensorType.Light);
#endif
        }

        /// <summary>
        /// 开启监听
        /// </summary>
        public void Start()
        {
#if ANDROID
            if (_lightSensor != null)
            {
                _sensorManager?.RegisterListener(this, _lightSensor, SensorDelay.Ui);
            }
#endif
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
#if ANDROID
            _sensorManager?.UnregisterListener(this);
#endif
        }

        /// <summary>
        /// 核心判定方法：环境是否昏暗
        /// </summary>
        /// <returns>布尔值：true 表示太暗了，false 表示光线充足</returns>
        public bool IsDim()
        {
            return _lastReading < DimLuxThreshold;
        }

        /// <summary>
        /// 获取当前的 Lux 数值
        /// </summary>
        public float GetCurrentLux() => _lastReading;

#if ANDROID
        // 必须实现：传感器精度改变时的处理
        public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy) { }

        // 必须实现：当光线数值变化时触发
        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Sensor?.Type == SensorType.Light && e.Values != null && e.Values.Count > 0)
            {
                _lastReading = e.Values[0];
            }
        }
#endif
    }
}