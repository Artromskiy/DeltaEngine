using System.Text.Json.Nodes;

namespace DeltaEditor
{
    internal class EntityView
    {
        private void DoSomething()
        {
            JsonObject[] jArray = [];
            /*
            foreach (JsonObject element in jArray)
            {
                string type = element["type"].ToString();
                TextBlock textBlock = new TextBlock() { Text = element["name"].ToString() };
                textBlock.Padding = new Thickness() { Top = 5 };
                switch (type)
                {
                    case "hiddendata":
                        break;
                    case "bool":
                        CheckBox checkBox = new CheckBox();
                        checkBox.SetValue(element);
                        //Binding checkBoxBinding = new Binding() { Path = new PropertyPath("[value].Value"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                        checkBoxBinding.Source = element;
                        checkBox.SetBinding(CheckBox.IsCheckedProperty, checkBoxBinding);
                        stackPanel.Children.Add(textBlock);
                        stackPanel.Children.Add(checkBox);
                        break;
                    case "image":
                        if (!string.IsNullOrEmpty(element["value"].Value<string>()))
                        {
                            Image image = new Image();
                            image.MaxHeight = 200;
                            image.MaxWidth = 200;
                            var ignore = SetImageSource(element["value"].Value<String>(), image);
                            stackPanel.Children.Add(textBlock);
                            stackPanel.Children.Add(image);
                        }
                        break;
                    case "info":
                        if (!String.IsNullOrEmpty(element["value"].Value<String>()))
                        {
                            TextBlock displayTextBlock = new TextBlock();
                            displayTextBlock.DataContext = element;
                            Binding displayTextBlockBinding = new Binding() { Path = new PropertyPath("[value].Value"), Mode = BindingMode.OneWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                            displayTextBlockBinding.Source = element;
                            displayTextBlock.SetBinding(TextBlock.TextProperty, displayTextBlockBinding);
                            stackPanel.Children.Add(textBlock);
                            stackPanel.Children.Add(displayTextBlock);
                        }
                        break;
                    case "password":
                        PasswordBox passwordBox = new PasswordBox();
                        passwordBox.DataContext = element;
                        Binding passwordBoxBinding = new Binding() { Path = new PropertyPath("[value].Value"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                        passwordBoxBinding.Source = element;
                        passwordBox.SetBinding(PasswordBox.PasswordProperty, passwordBoxBinding);
                        stackPanel.Children.Add(textBlock);
                        stackPanel.Children.Add(passwordBox);
                        break;
                    case "string":
                    default:
                        TextBox textBox = new TextBox();
                        textBox.DataContext = element;
                        Binding textBoxBinding = new Binding() { Path = new PropertyPath("[value].Value"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                        textBoxBinding.Source = element;
                        textBox.SetBinding(TextBox.TextProperty, textBoxBinding);
                        stackPanel.Children.Add(textBlock);
                        stackPanel.Children.Add(textBox);
                        break;
                }
            }
            */
        }
    }
}
