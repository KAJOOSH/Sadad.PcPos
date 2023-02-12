using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PcPosSampleDll
{
    internal class TreeViewEx : TreeView
    {
        public const int TVIF_STATE = 0x8;
        public const int TVIS_STATEIMAGEMASK = 0xF000;
        public const int TV_FIRST = 0x1100;
        public const int TVM_SETITEM = TV_FIRST + 63;
        public struct TreeViewItem
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
#pragma warning disable 0649
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
#pragma warning restore 0649
        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        //protected override void WndProc(ref Message m)
        //{
        //    if (m.Msg != 0x201 && m.Msg != 0x204)
        //    {
        //        base.WndProc(ref m);
        //    }
        //}

        //public void AddItemEx(string text)
        //{
        //    TreeViewItem tvi = new TreeViewItem();
        //    tvi.hItem = this.SelectedNode.Handle;
        //    tvi.mask = TVIF_STATE;
        //    tvi.stateMask = TVIS_STATEIMAGEMASK;
        //    tvi.state = 0;
        //    IntPtr lparam = Marshal.AllocHGlobal(Marshal.SizeOf(tvi));
        //    Marshal.StructureToPtr(tvi, lparam, false);
        //    SendMessage(this.Handle, TVM_SETITEM, IntPtr.Zero, lparam);
        //}

        public TreeViewEx()
        {
            BeforeCheck += TreeViewEx_BeforeCheck;
        }

        private void TreeViewEx_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Parent != null)//==).state == TVIS_STATEIMAGEMASK)
            {
                e.Cancel = true;
            }
        }

        internal void HideCheckBox(TreeNode node)
        {
            TreeViewItem tvi = new TreeViewItem();
            tvi.hItem = node.Handle;
            tvi.mask = TVIF_STATE;
            tvi.stateMask = TVIS_STATEIMAGEMASK;
            tvi.state = 0;
            IntPtr lparam = Marshal.AllocHGlobal(Marshal.SizeOf(tvi));
            Marshal.StructureToPtr(tvi, lparam, false);
            SendMessage(node.TreeView.Handle, TVM_SETITEM, IntPtr.Zero, lparam);
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            base.OnDrawNode(e);
            if (e.Node.Level == 1)
            {
                HideCheckBox(e.Node);
                e.DrawDefault = true;
            }
            else
            {
                e.Graphics.DrawString(e.Node.Text, e.Node.TreeView.Font,
                   Brushes.Black, e.Node.Bounds.X, e.Node.Bounds.Y);
            }
        }
    }
}
