using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace PhpTestTemplateGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void txtContent_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void txtContent_Drop(object sender, DragEventArgs e)
        {
            var errorMessage = "";
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                foreach (var file in files)
                {
                    var fileTitle = Path.GetFileNameWithoutExtension(file);
                    var content = File.ReadAllText(file);
                    var className = GetClassName(content);
                    if (className == fileTitle)
                    {
                        var testFileName = fileTitle + "Test.php";
                        txtOutputFileName.Text = testFileName;

                        var outputContent = GenerateTestTemplate(content, className);
                        txtContent.Text = outputContent;

                        errorMessage = GetError();
                    }
                    else
                    {
                        errorMessage = "ファイル名とクラス名が一致しません。";
                    }
                    break;
                }
            }
            lblMessage.Content = errorMessage;
        }

        private string GetError()
        {
            if (string.IsNullOrEmpty(txtOutputFolder.Text))
            {
                return "出力フォルダが未入力です。";
            }
            if (!Directory.Exists(txtOutputFolder.Text))
            {
                return "出力フォルダがありません。";
            }
            if (File.Exists(Path.Combine(txtOutputFolder.Text, txtOutputFileName.Text)))
            {
                return "テストファイルは既に存在しています。";
            }

            return "";
        }

        private string? GetClassName(string content)
        {
            var m = Regex.Match(content, @"class (\w+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private string GenerateTestTemplate(string content, string className)
        {
            var functionMatches = Regex.Matches(content, @"public\s+(?<static>static\s+)?function\s+(?<name>\w+)\s*(?<params>\([^)]*\))");
            var namespaceMatch = Regex.Match(content, @"namespace \w+;");
            var sb = new StringBuilder();
            sb.AppendLine(@"<?php");
            sb.AppendLine();
            if (namespaceMatch.Success)
            {
                sb.AppendLine(namespaceMatch.Value);
                sb.AppendLine();
            }
            sb.AppendLine(@"use PHPUnit\Framework\Assert;");
            sb.AppendLine();
            sb.AppendLine(@$"class {className}Test extends \PHPUnit\Framework\TestCase");
            sb.AppendLine(@"{");
            var first = true;
            foreach (Match m in functionMatches)
            {
                var function = m.Groups["name"].Value;
                if (function.StartsWith("__"))
                {
                    continue;
                }
                if (!first)
                {
                    sb.AppendLine();
                }
                sb.AppendLine(@$"    public function test{function.Substring(0, 1).ToUpper()}{function.Substring(1)}()");
                sb.AppendLine(@"    {");
                sb.AppendLine(@$"        // TODO: {(m.Groups["static"].Success ? $"{className}::" : "$obj->")}{function}{m.Groups["params"].Value} のテストを記述します(#XXX)");
                sb.AppendLine(@"        Assert::markTestIncomplete();");
                sb.AppendLine(@"    }");
                first = false;
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var errorMessage = GetError();
            if (string.IsNullOrEmpty(errorMessage))
            {
                File.WriteAllText(Path.Combine(txtOutputFolder.Text, txtOutputFileName.Text), txtContent.Text.Replace("\r\n", "\n"));
                lblMessage.Content = "保存しました。";
            }
            else
            {
                lblMessage.Content = errorMessage;
            }
        }
    }
}
