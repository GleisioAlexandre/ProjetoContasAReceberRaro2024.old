﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ContasAReceber.controller;
using ContasAReceber.model;
using FirebirdSql.Data.FirebirdClient;
using PagedList;
using System.Drawing.Printing;
using System.IO;
using System.Diagnostics;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Font = iTextSharp.text.Font;

namespace ContasAReceber.View
{
    public partial class FrmContas : Form
    {
        OperacoesContas op = new OperacoesContas();
        public FrmContas()
        {
            InitializeComponent();
        }
        private void FrmContas_Load(object sender, EventArgs e)
        {
            try
            {
                AtualizaGridContas();
                toolStripComboBox1.SelectedText = "Todos";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }
        private void CorGrid()
        {

            int colunaIndex = 5;
            foreach (DataGridViewRow row in dtgContas.Rows)
            {
                if (!row.IsNewRow)
                {
                    DataGridViewCell celula = row.Cells[colunaIndex];

                    string textocelula = celula.Value.ToString();
                    if (textocelula == "Pago")
                    {
                        row.DefaultCellStyle.BackColor = Color.Green;
                    }

                }
            }
        }
        public void AtualizaGridContas()
        {
            bindingSource1.DataSource = op.datSet().Tables["contasareceber"];
            dtgContas.DataSource = bindingSource1;
            toolStripTextBox1.Clear();
        }
        private void FrmContas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Insert)
            {
                FrmOperacoesContas frmInserirContas = new FrmOperacoesContas(this);
                frmInserirContas.ShowDialog();
            }
        }
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {

            bindingSource1.DataSource = op.datSet().Tables["contasareceber"];
            string filtro = toolStripTextBox1.Text;
            if (bindingSource1 != null)
            {
                bindingSource1.Filter = string.Format("nome like '%{0}%'", filtro);
            }
        }
        private void dtgContas_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
           if (e.RowIndex >= 0 && e.RowIndex < dtgContas.Rows.Count)
            {
                DataGridViewRow linhaClicada = dtgContas.Rows[e.RowIndex];

                int indiceDaColuna = 3;

                object valorDaCelula = linhaClicada.Cells[indiceDaColuna].Value;

                if (valorDaCelula != null)
                {
                    string textoDaCelula = valorDaCelula.ToString();
                    FrmOperacoesContas operacoesContas = new FrmOperacoesContas(this);
                    operacoesContas.DadosDoFormContas(textoDaCelula);
                    operacoesContas.ShowDialog();
                }
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string situacao = toolStripComboBox1.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(situacao))
            {
                bindingSource1.RemoveFilter();
            }
            else if (situacao.Equals("Todos"))
            {
                bindingSource1.RemoveFilter();
            }
            else
            {
                bindingSource1.Filter = $"situacao = '{situacao}'";
            }
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            GerarRelatorio();

        }
        private void GerarRelatorio()
        {
            Document doc = new Document(PageSize.A4);
            doc.SetMargins(30, 30, 30, 30);
            doc.AddCreationDate();
            string caminho = @"E:\Desktop\bd\relatorio.pdf";

            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(caminho, FileMode.Create));
            BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            doc.Open();

            //// Criando uma fonte para o cabeçalho da tabela
            Font fontTitulo = new Font(baseFont, 30);


            //Cria o titulo da tebla
            Paragraph titulo = new Paragraph();
            titulo.Alignment = Element.ALIGN_CENTER;
            titulo.Font = fontTitulo;
            titulo.Add("\n\nRelatório de Clientes " + "(" + toolStripComboBox1.SelectedText.ToString() + ")" + "\n\n");
            doc.Add(titulo);

            // Criando uma fonte para o conteúdo da tabela
            Font fontCabecalho = new Font(baseFont, 8);
            Font fontConteudo = new Font(baseFont, 7);

            // Criando a tabela
            PdfPTable table = new PdfPTable(dtgContas.Columns.Count);
            table.WidthPercentage = 105;

            // Definindo a largura das colunas
            float[] larguraColuna = new float[] { 1f, 4f, 1f, 1f, 0f, 0f, 1f, 1f };
            table.SetWidths(larguraColuna);

            // Adicionando cabeçalhos da tabela
            foreach (DataGridViewColumn column in dtgContas.Columns)
            {
                PdfPCell cell = new PdfPCell(new Phrase(column.HeaderText, fontCabecalho)); // Usando a fonte para o cabeçalho
                cell.BackgroundColor = new BaseColor(240, 240, 240);
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(cell);
            }

            // Adicionando dados à tabela
            foreach (DataGridViewRow row in dtgContas.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    PdfPCell cellPdf = new PdfPCell(new Phrase(cell.Value != null ? cell.Value.ToString() : string.Empty, fontConteudo));
                    if (cell.OwningColumn.Name == "valor")
                    {
                        string valorMonetario = string.Format("{0:C}", cell.Value != null ? cell.Value : 0);
                        cellPdf = new PdfPCell(new Phrase(valorMonetario, fontConteudo));
                        cellPdf.HorizontalAlignment = Element.ALIGN_RIGHT;
                    }
                    else if (cell.OwningColumn.Name == "documento")
                    {
                        cellPdf.HorizontalAlignment = Element.ALIGN_CENTER;
                    }

                    else if (cell.OwningColumn.Name == "entrada" || cell.OwningColumn.Name == "vencimento" || cell.OwningColumn.Name == "pagamento")
                    {
                        string dataFormatada = (cell.Value != null && cell.Value != DBNull.Value && DateTime.TryParse(cell.Value.ToString(), out DateTime data)) ? data.ToString("dd/MM/yyyy") : string.Empty;
                        cellPdf = new PdfPCell(new Phrase(dataFormatada, fontCabecalho));
                        cellPdf.HorizontalAlignment = Element.ALIGN_CENTER;

                    }
                    else
                    {
                        cellPdf = new PdfPCell(new Phrase(cell.Value != null ? cell.Value.ToString() : string.Empty, fontConteudo)); // Usando a fonte para o conteúdo

                    }
                    table.AddCell(cellPdf);
                }
            }

            // Adicionando a tabela ao documento
            doc.Add(table);
            doc.Close();
            writer.Close();
            AbrirPdf(caminho);
        }
        private void AbrirPdf(string caminhoPdf)
        {
            try
            {
                if (File.Exists(caminhoPdf))
                {
                    Process.Start(caminhoPdf);
                }
                else
                {
                    MessageBox.Show("O arquivo PDF não foi encontrado!", "Erro ao Abrir o PDF");

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o PDF: {ex.Message}", "Erro ao abrir o PDF");
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (dtgContas.Rows[dtgContas.NewRowIndex - 1].IsNewRow)
            {

            }
            string entrada = dtgContas.Rows[dtgContas.NewRowIndex].Cells["entrada"].Value.ToString(); ;
            Console.WriteLine(entrada);
            /*bindingSource1.EndEdit();
            op.InserirConta();*/
        }
    }
}


