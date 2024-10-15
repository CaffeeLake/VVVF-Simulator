﻿using System;
using System.Globalization;
using System.Windows.Data;
using VvvfSimulator.GUI.Resource.Language;
using static VvvfSimulator.Yaml.VvvfSound.YamlVvvfSoundData.YamlControlData;

namespace VvvfSimulator.GUI.Resource.Converter
{
    public class PulseModeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not YamlPulseMode Mode) throw new NotImplementedException();
            return FriendlyNameConverter.GetPulseModeName(Mode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
