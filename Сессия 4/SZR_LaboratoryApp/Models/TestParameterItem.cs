using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SZR_LaboratoryApp.Models
{
    public class TestParameterItem : INotifyPropertyChanged
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parameterName")]
        public string parameterName { get; set; }

        [JsonProperty("normMin")]
        public decimal? normMin { get; set; }

        [JsonProperty("normMax")]
        public decimal? normMax { get; set; }

        [JsonProperty("unit")]
        public string unit { get; set; }

        private decimal? _actualValue;
        [JsonProperty("actualValue")]
        public decimal? ActualValue
        {
            get => _actualValue;
            set { _actualValue = value; Evaluate(); OnPropertyChanged(); }
        }

        private bool? _isPassed;
        [JsonProperty("isPassed")]
        public bool? IsPassed
        {
            get => _isPassed;
            private set { _isPassed = value; OnPropertyChanged(); }
        }

        private void Evaluate()
        {
            if (!ActualValue.HasValue) { IsPassed = null; return; }
            bool ok = true;
            if (normMin.HasValue) ok &= ActualValue.Value >= normMin.Value;
            if (normMax.HasValue) ok &= ActualValue.Value <= normMax.Value;
            IsPassed = ok;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}