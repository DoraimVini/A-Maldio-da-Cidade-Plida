import os
import re

docs_dir = r'c:\Users\Vini\Desktop\projeto_amarelo_unity\Docs\KnowledgeBundle'
files_to_bundle = [
    ('1. Documento de Design Mestre (GDD)', r'GDD_Mestre.md'),
    ('2. GDD da Expansão e Demo do Deserto', r'gdd_expansao_deserto_demo.md'),
    ('3. Lore: O Povo Serpente', r'lore\povo_serpente.md'),
    ('4. Lore: O Templo da Serpente (Dungeon 2)', r'lore\templo_da_serpente.md'),
    ('5. Lore: Rainha Cassilda, Quest e Boss Byakhee', r'lore\cassilda_e_byakhee.md'),
    ('6. Lore & Mecânica: Yug-Neth (Mi-Go Companion)', r'lore\migo_companion.md'),
    ('7. Lore & Boss: Abdul Alhazred', r'lore\abdul_alhazred.md'),
    ('8. Lore: Deserto de Hali e Dungeons', r'lore\deserto_e_dungeons.md'),
    ('9. Lore: As Quatro Relíquias Lendárias', r'lore\reliquias_cosmicas.md'),
    ('10. Lore: Glossário Diegético', r'lore\glossary.md'),
    ('11. Level Design: Overworld do Deserto de Hali', r'systems\level_design_deserto_hali.md'),
    ('12. Level Design: Boss Abdul Alhazred', r'systems\boss_abdul.md'),
]

def md_to_html(text):
    # Strip frontmatter
    text = re.sub(r'^---[\s\S]*?---\n', '', text)
    # Headings
    text = re.sub(r'^#### (.*?)$', r'<h4>\1</h4>', text, flags=re.M)
    text = re.sub(r'^### (.*?)$', r'<h3>\1</h3>', text, flags=re.M)
    text = re.sub(r'^## (.*?)$', r'<h2>\1</h2>', text, flags=re.M)
    text = re.sub(r'^# (.*?)$', r'<h1>\1</h1>', text, flags=re.M)
    # Bold / Italic
    text = re.sub(r'\*\*\*(.*?)\*\*\*', r'<strong><em>\1</em></strong>', text)
    text = re.sub(r'\*\*(.*?)\*\*', r'<strong>\1</strong>', text)
    text = re.sub(r'\*(.*?)\*', r'<em>\1</em>', text)
    # Blockquotes
    text = re.sub(r'^> (.*?)$', r'<blockquote>\1</blockquote>', text, flags=re.M)
    # Tables simple converter
    lines = text.split('\n')
    in_table = False
    new_lines = []
    for line in lines:
        if '|' in line:
            if '---' in line:
                continue
            cells = [c.strip() for c in line.split('|')[1:-1]]
            if not in_table:
                in_table = True
                new_lines.append('<table class="doc-table">')
                new_lines.append('<tr>' + ''.join(f'<th>{c}</th>' for c in cells) + '</tr>')
            else:
                new_lines.append('<tr>' + ''.join(f'<td>{c}</td>' for c in cells) + '</tr>')
        else:
            if in_table:
                in_table = False
                new_lines.append('</table>')
            new_lines.append(line)
    if in_table:
        new_lines.append('</table>')
    text = '\n'.join(new_lines)
    # Code blocks
    text = re.sub(r'```([\s\S]*?)```', r'<pre><code>\1</code></pre>', text)
    text = re.sub(r'`(.*?)`', r'<code>\1</code>', text)
    # Paragraphs
    paragraphs = text.split('\n\n')
    p_html = []
    for p in paragraphs:
        p = p.strip()
        if not p: continue
        if p.startswith('<h') or p.startswith('<table') or p.startswith('<blockquote') or p.startswith('<pre'):
            p_html.append(p)
        else:
            p_html.append(f'<p>{p.replace(chr(10), "<br>")}</p>')
    return '\n'.join(p_html)

sections_html = []
toc_html = []

for idx, (title, rel_path) in enumerate(files_to_bundle, 1):
    full_path = os.path.join(docs_dir, rel_path)
    if os.path.exists(full_path):
        with open(full_path, 'r', encoding='utf-8') as f:
            content = f.read()
        body = md_to_html(content)
        sec_id = f'section-{idx}'
        toc_html.append(f'<li><a href="#{sec_id}">{title}</a></li>')
        sections_html.append(f'<section id="{sec_id}" class="doc-section"><div class="section-header"><span>CAPÍTULO {idx}</span><h2>{title}</h2></div>{body}</section>')

html_doc = f'''<!DOCTYPE html>
<html lang="pt-BR">
<head>
<meta charset="UTF-8">
<title>A Maldição da Cidade Pálida — Documentação Mestra</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Cinzel:wght@700&family=Inter:wght@400;600;700&family=Fira+Code&display=swap');

@page {{
    size: A4;
    margin: 20mm 15mm 20mm 15mm;
}}

body {{
    font-family: 'Inter', -apple-system, sans-serif;
    color: #1a1a1a;
    background-color: #ffffff;
    line-height: 1.6;
    margin: 0;
    padding: 40px;
}}

.cover {{
    height: 85vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    text-align: center;
    page-break-after: always;
    border-bottom: 4px solid #1a1a1a;
    margin-bottom: 40px;
}}

.cover h1 {{
    font-family: 'Cinzel', serif;
    font-size: 34pt;
    margin: 0 0 15px 0;
    letter-spacing: 2px;
    color: #111;
}}

.cover h2 {{
    font-size: 16pt;
    font-weight: 400;
    color: #444;
    margin-bottom: 50px;
}}

.cover .meta {{
    font-size: 11pt;
    color: #555;
    border-top: 2px solid #eee;
    padding-top: 25px;
    width: 70%;
    line-height: 1.8;
}}

.toc {{
    background: #f8f9fa;
    border: 1px solid #e9ecef;
    border-radius: 8px;
    padding: 35px;
    margin-bottom: 50px;
    page-break-after: always;
}}

.toc h2 {{
    font-family: 'Cinzel', serif;
    margin-top: 0;
    border-bottom: 2px solid #111;
    padding-bottom: 10px;
    font-size: 18pt;
}}

.toc ul {{
    list-style: none;
    padding-left: 0;
}}

.toc li {{
    margin-bottom: 12px;
    font-size: 11pt;
}}

.toc a {{
    color: #0044cc;
    text-decoration: none;
    font-weight: 600;
}}

.doc-section {{
    page-break-after: always;
    margin-bottom: 60px;
}}

.section-header {{
    border-bottom: 3px solid #111;
    padding-bottom: 8px;
    margin-bottom: 25px;
}}

.section-header span {{
    font-size: 9pt;
    font-weight: 700;
    letter-spacing: 1.5px;
    color: #666;
    display: block;
}}

.section-header h2 {{
    font-family: 'Cinzel', serif;
    font-size: 20pt;
    margin: 0;
}}

h1, h2, h3, h4 {{
    color: #111;
}}

h3 {{ font-size: 14pt; margin-top: 25px; border-bottom: 1px solid #eee; padding-bottom: 5px; }}
h4 {{ font-size: 11pt; margin-top: 20px; }}

blockquote {{
    background: #f8f9fa;
    border-left: 4px solid #111;
    margin: 18px 0;
    padding: 14px 20px;
    font-style: italic;
    color: #333;
}}

table.doc-table {{
    width: 100%;
    border-collapse: collapse;
    margin: 20px 0;
    font-size: 10pt;
}}

table.doc-table th, table.doc-table td {{
    border: 1px solid #ddd;
    padding: 10px 12px;
    text-align: left;
}}

table.doc-table th {{
    background: #f1f3f5;
    font-weight: 700;
}}

code {{
    font-family: 'Fira Code', monospace;
    background: #f1f3f5;
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 9.5pt;
}}

pre code {{
    display: block;
    padding: 15px;
    background: #1a1a1a;
    color: #f8f9fa;
    border-radius: 6px;
    overflow-x: auto;
    white-space: pre-wrap;
}}

@media print {{
    body {{ padding: 0; background: #fff; }}
    .cover {{ height: 100vh; }}
}}
</style>
</head>
<body>

<div class="cover">
    <h1>A MALDIÇÃO DA CIDADE PÁLIDA</h1>
    <h2>Documentação Mestra de Design & Game Design Document (GDD)</h2>
    <div class="meta">
        <p><strong>Projeto:</strong> Favela Amarela / A Maldição da Cidade Pálida</p>
        <p><strong>Versão:</strong> 1.2 — Vertical Slice / Edital</p>
        <p><strong>Data de Emissão:</strong> Julho de 2026</p>
        <p><strong>Autoria:</strong> Vinícius (Vini) & Equipe de Desenvolvimento</p>
        <p><strong>Finalidade:</strong> Apresentação Executiva para Direção e Edital</p>
    </div>
</div>

<div class="toc">
    <h2>ÍNDICE GERAL DE CONTEÚDO</h2>
    <ul>
        {''.join(toc_html)}
    </ul>
</div>

{''.join(sections_html)}

</body>
</html>
'''

out_path = r'c:\Users\Vini\Desktop\projeto_amarelo_unity\Docs\Documentacao_Mestra_Favela_Amarela.html'
with open(out_path, 'w', encoding='utf-8') as f:
    f.write(html_doc)

print(f'Documento mestre compilado com sucesso em:\n{out_path}')
