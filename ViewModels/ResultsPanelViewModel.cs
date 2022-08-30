using FFXIVOpcodeWizard.PacketDetection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace FFXIVOpcodeWizard.ViewModels
{
    public class ResultsPanelViewModel : INotifyPropertyChanged
    {
        private string contents;
        public string Contents
        {
            get => this.contents;
            set
            {
                this.contents = value;
                OnPropertyChanged();
            }
        }

        private string affix;
        public string Affix
        {
            get => this.affix;
            set
            {
                this.affix = value;
                OnPropertyChanged();
                UpdateContents();
            }
        }

        private ScannerRegistry registry;
        private NumberFormatSelectorViewModel numberFormatSelector;

        public void Load(ScannerRegistry registry, NumberFormatSelectorViewModel numberFormatSelector)
        {
            this.registry = registry;
            this.numberFormatSelector = numberFormatSelector;

            this.affix = "";
            this.contents = "";
        }

        public void UpdateContents()
        {
            var serverSb = new StringBuilder();
            var clientSb = new StringBuilder();

            var format = this.numberFormatSelector.SelectedFormat;
            var client = new Dictionary<string, int>();
            var server = new Dictionary<string, int>();
            foreach (var scanner in this.registry.AsList())
            {
                if (scanner.Opcode == 0) continue;

                //var sb = scanner.PacketSource == PacketSource.Client ? clientSb : serverSb;

                //sb.Append(scanner.PacketName).Append(" = ")
                //    .Append(Util.NumberToString(scanner.Opcode, format)).Append(",").Append(Affix);
                //if (scanner.Comment.Text != null)
                //{
                //    sb.Append(" (").Append(scanner.Comment).Append(")");
                //}

                //sb.AppendLine();
                if (scanner.PacketSource == PacketSource.Client)
                {
                    client.Add(scanner.PacketName, scanner.Opcode);
                }
                else {
                    server.Add(scanner.PacketName, scanner.Opcode);
                }
            }

            var resultSb = new StringBuilder();
            resultSb.AppendLine("// Server Zone");
            resultSb.AppendLine(JsonSerializer.Serialize(server,new JsonSerializerOptions() { WriteIndented =true}));
            //resultSb.Append(serverSb);
            resultSb.AppendLine();
            resultSb.AppendLine("// Client Zone");
            resultSb.AppendLine(JsonSerializer.Serialize(client, new JsonSerializerOptions() { WriteIndented = true }));
            resultSb.AppendLine();
            resultSb.Append(clientSb);
            resultSb.AppendLine();

            Contents = resultSb.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}