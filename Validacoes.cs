using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>Validações e máscaras de digitação (CPF/CNPJ, telefone, placa, CEP, valor).</summary>
    public static class Validacoes
    {
        public static bool SomenteDigitos(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        public static string SoDigitos(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s) if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        // ===== Validações =====

        public static bool CpfValido(string cpf)
        {
            string d = SoDigitos(cpf);
            if (d.Length != 11) return false;
            bool iguais = true;
            for (int i = 1; i < d.Length; i++) if (d[i] != d[0]) { iguais = false; break; }
            if (iguais) return false;
            int[] p1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] p2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            return DigitoVerificador(d, p1, 10) && DigitoVerificador(d, p2, 11);
        }

        public static bool CnpjValido(string cnpj)
        {
            string d = SoDigitos(cnpj);
            if (d.Length != 14) return false;
            bool iguais = true;
            for (int i = 1; i < d.Length; i++) if (d[i] != d[0]) { iguais = false; break; }
            if (iguais) return false;
            int[] p1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] p2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            return DigitoVerificador(d, p1, 5) && DigitoVerificador(d, p2, 6);
        }

        private static bool DigitoVerificador(string digitos, int[] pesos, int restoBase)
        {
            int soma = 0;
            for (int i = 0; i < pesos.Length; i++)
                soma += (digitos[i] - '0') * pesos[i];
            int resto = soma % 11;
            int dv = resto < 2 ? 0 : 11 - resto;
            return dv == (digitos[pesos.Length] - '0');
        }

        // ===== Máscaras aplicadas a cada KeyPress =====

        public static void AplicarMascaraCpfCnpj(TextBox t)
        {
            string d = SoDigitos(t.Text);
            if (d.Length <= 11)
            {
                if (d.Length > 11) d = d.Substring(0, 11);
                t.Text = d.Length <= 11 ? FormatarCpf(d) : "";
            }
            else
            {
                if (d.Length > 14) d = d.Substring(0, 14);
                t.Text = FormatarCnpj(d);
            }
            t.SelectionStart = t.Text.Length;
        }

        public static string FormatarCpf(string d)
        {
            if (d.Length <= 3) return d;
            if (d.Length <= 6) return d.Substring(0, 3) + "." + d.Substring(3);
            if (d.Length <= 9) return d.Substring(0, 3) + "." + d.Substring(3, 3) + "." + d.Substring(6);
            return d.Substring(0, 3) + "." + d.Substring(3, 3) + "." + d.Substring(6, 3) + "-" + d.Substring(9);
        }

        public static string FormatarCnpj(string d)
        {
            if (d.Length <= 2) return d;
            if (d.Length <= 5) return d.Substring(0, 2) + "." + d.Substring(2);
            if (d.Length <= 8) return d.Substring(0, 2) + "." + d.Substring(2, 3) + "." + d.Substring(5);
            if (d.Length <= 12) return d.Substring(0, 2) + "." + d.Substring(2, 3) + "." + d.Substring(5, 3) + "/" + d.Substring(8);
            return d.Substring(0, 2) + "." + d.Substring(2, 3) + "." + d.Substring(5, 3) + "/" + d.Substring(8, 4) + "-" + d.Substring(12);
        }

        public static void AplicarMascaraTelefone(TextBox t)
        {
            string d = SoDigitos(t.Text);
            if (d.Length > 11) d = d.Substring(0, 11);
            if (d.Length <= 2) t.Text = d.Length == 0 ? "" : "(" + d;
            else if (d.Length <= 6) t.Text = "(" + d.Substring(0, 2) + ") " + d.Substring(2);
            else if (d.Length <= 10) t.Text = "(" + d.Substring(0, 2) + ") " + d.Substring(2, 4) + "-" + d.Substring(6);
            else t.Text = "(" + d.Substring(0, 2) + ") " + d.Substring(2, 5) + "-" + d.Substring(7);
            t.SelectionStart = t.Text.Length;
        }

        public static void AplicarMascaraCep(TextBox t)
        {
            string d = SoDigitos(t.Text);
            if (d.Length > 8) d = d.Substring(0, 8);
            t.Text = d.Length <= 5 ? d : d.Substring(0, 5) + "-" + d.Substring(5);
            t.SelectionStart = t.Text.Length;
        }

        public static void AplicarMascaraPlaca(TextBox t)
        {
            string s = Regex.Replace(t.Text.ToUpper(), "[^A-Z0-9]", "");
            if (s.Length > 7) s = s.Substring(0, 7);
            // Mercosul: ABC1D23  |  antiga: ABC-1234
            if (s.Length <= 3) t.Text = s;
            else if (s.Length <= 6) t.Text = s.Substring(0, 3) + "-" + s.Substring(3);
            else t.Text = s.Substring(0, 3) + "-" + s.Substring(3);
            t.SelectionStart = t.Text.Length;
        }

        public static void AplicarMascaraValor(TextBox t)
        {
            string d = SoDigitos(t.Text);
            if (d.Length == 0) { t.Text = "0,00"; t.SelectionStart = t.Text.Length; return; }
            if (d.Length == 1) d = "0" + d;
            if (d.Length == 2) d = "0" + d;
            decimal valor;
            if (decimal.TryParse(d.Substring(0, d.Length - 2) + "," + d.Substring(d.Length - 2),
                NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor))
            {
                t.Text = valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
                t.SelectionStart = t.Text.Length;
            }
        }

        public static decimal LerValor(TextBox t)
        {
            decimal v;
            if (decimal.TryParse(t.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out v))
                return v;
            return 0m;
        }
    }
}