using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>
    /// Gerenciador de instâncias de janelas: garante que cada módulo abra
    /// uma única vez por vez. Ao abrir uma janela já existente, apenas a
    /// traz para frente (evita telas duplicadas e gravações em duplicidade).
    /// </summary>
    public static class Janelas
    {
        private static readonly Dictionary<Type, Form> _abertas = new Dictionary<Type, Form>();

        /// <summary>
        /// Abre a janela, focando a existente caso já esteja aberta.
        /// Janelas modais (ShowDialog) NÃO devem passar por aqui.
        /// </summary>
        public static T Abrir<T>(Func<T> criar) where T : Form
        {
            Form existente;
            if (_abertas.TryGetValue(typeof(T), out existente) && existente != null && !existente.IsDisposed)
            {
                if (existente.WindowState == FormWindowState.Minimized)
                    existente.WindowState = FormWindowState.Normal;
                existente.BringToFront();
                existente.Activate();
                return existente as T;
            }

            Form nova = criar();
            nova.FormClosed += (s, e) =>
            {
                if (_abertas.ContainsKey(typeof(T)))
                    _abertas.Remove(typeof(T));
            };
            _abertas[typeof(T)] = nova;
            nova.Show();
            return nova as T;
        }
    }
}