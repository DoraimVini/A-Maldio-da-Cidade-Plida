import urllib.request
import json
import os
import re

token = os.environ.get('NOTION_TOKEN')
if not token:
    raise SystemExit(
        "NOTION_TOKEN não definido. Defina a variável de ambiente antes de rodar este "
        "script (ex.: PowerShell: $env:NOTION_TOKEN = 'ntn_...'). O token antigo hardcoded "
        "aqui foi removido por ter vazado em texto puro no controle de versão — rotacione-o "
        "nas configurações da integração do Notion antes de gerar um novo."
    )
headers = {
    'Authorization': f'Bearer {token}',
    'Notion-Version': '2022-06-28',
    'Content-Type': 'application/json'
}

page_ids = [
    '3ad05465-f1ab-8066-940a-e4278c78bbf2',
    '3ad05465-f1ab-8082-8779-c42d9ef13181'
]

def get_block_children(block_id):
    url = f'https://api.notion.com/v1/blocks/{block_id}/children?page_size=100'
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req) as resp:
            return json.loads(resp.read().decode('utf-8')).get('results', [])
    except Exception as e:
        print("Erro ao buscar filhos:", e)
        return []

def delete_block(block_id):
    url = f'https://api.notion.com/v1/blocks/{block_id}'
    req = urllib.request.Request(url, headers=headers, method='DELETE')
    try:
        with urllib.request.urlopen(req) as resp:
            pass
    except Exception as e:
        print(f"Erro ao deletar bloco {block_id}:", e)

def clear_page(page_id):
    print("Limpando blocos antigos da página...")
    total_removidos = 0
    while True:
        children = get_block_children(page_id)
        if not children:
            break
        for child in children:
            delete_block(child['id'])
        total_removidos += len(children)
        print(f"{len(children)} blocos removidos no lote atual...")
    print(f"Total de {total_removidos} blocos removidos.")

def create_rich_text(text):
    # Basic markdown bold/italic/code parsing for Notion rich_text objects
    text_obj = []
    # If text is simple, return standard text block
    if not text:
        return []
    
    # Simple chunking to avoid Notion 2000 char limit per text block
    chunks = [text[i:i+1900] for i in range(0, len(text), 1900)]
    rich_list = []
    for c in chunks:
        rich_list.append({
            'type': 'text',
            'text': {'content': c}
        })
    return rich_list

def parse_markdown_to_notion_blocks(md_content):
    lines = md_content.split('\n')
    blocks = []
    
    # Table of contents block at the very top
    blocks.append({
        'object': 'block',
        'type': 'table_of_contents',
        'table_of_contents': {'color': 'default'}
    })
    blocks.append({
        'object': 'block',
        'type': 'divider',
        'divider': {}
    })

    in_code = False
    code_text = []

    for line in lines:
        stripped = line.strip()
        
        # Code blocks
        if stripped.startswith('```'):
            if in_code:
                in_code = False
                full_code = '\n'.join(code_text)
                blocks.append({
                    'object': 'block',
                    'type': 'code',
                    'code': {
                        'rich_text': create_rich_text(full_code),
                        'language': 'markdown'
                    }
                })
                code_text = []
            else:
                in_code = True
                code_text = []
            continue
        
        if in_code:
            code_text.append(line)
            continue
            
        if not stripped:
            continue
            
        # Headings
        if stripped.startswith('# '):
            blocks.append({
                'object': 'block',
                'type': 'heading_1',
                'heading_1': {'rich_text': create_rich_text(stripped[2:])}
            })
        elif stripped.startswith('## '):
            blocks.append({
                'object': 'block',
                'type': 'heading_2',
                'heading_2': {'rich_text': create_rich_text(stripped[3:])}
            })
        elif stripped.startswith('### '):
            blocks.append({
                'object': 'block',
                'type': 'heading_3',
                'heading_3': {'rich_text': create_rich_text(stripped[4:])}
            })
        elif stripped == '---':
            blocks.append({
                'object': 'block',
                'type': 'divider',
                'divider': {}
            })
        elif stripped.startswith('> '):
            blocks.append({
                'object': 'block',
                'type': 'quote',
                'quote': {'rich_text': create_rich_text(stripped[2:])}
            })
        elif stripped.startswith('- ') or stripped.startswith('* '):
            blocks.append({
                'object': 'block',
                'type': 'bulleted_list_item',
                'bulleted_list_item': {'rich_text': create_rich_text(stripped[2:])}
            })
        elif re.match(r'^\d+\.\s', stripped):
            content_str = re.sub(r'^\d+\.\s', '', stripped)
            blocks.append({
                'object': 'block',
                'type': 'numbered_list_item',
                'numbered_list_item': {'rich_text': create_rich_text(content_str)}
            })
        else:
            # Paragraph
            blocks.append({
                'object': 'block',
                'type': 'paragraph',
                'paragraph': {'rich_text': create_rich_text(stripped)}
            })
            
    return blocks

def append_blocks_in_batches(page_id, blocks):
    url = f'https://api.notion.com/v1/blocks/{page_id}/children'
    batch_size = 50
    print(f"Enviando {len(blocks)} blocos em lotes para o Notion...")
    
    for i in range(0, len(blocks), batch_size):
        batch = blocks[i:i+batch_size]
        data = json.dumps({'children': batch}).encode('utf-8')
        req = urllib.request.Request(url, data=data, headers=headers, method='PATCH')
        try:
            with urllib.request.urlopen(req) as resp:
                print(f"Lote {i//batch_size + 1} ({len(batch)} blocos) enviado com sucesso.")
        except Exception as e:
            print(f"Erro ao enviar lote {i//batch_size + 1}:", e)

def sync_gdd_to_notion():
    gdd_file = r'c:\Users\Vini\Desktop\projeto_amarelo_unity\Docs\GDD_Unificado_Favela_Amarela.md'
    if not os.path.exists(gdd_file):
        print("Arquivo GDD não encontrado.")
        return
        
    with open(gdd_file, 'r', encoding='utf-8') as f:
        md_content = f.read()
        
    blocks = parse_markdown_to_notion_blocks(md_content)
    
    for pid in page_ids:
        print(f"\n--- Sincronizando Página ID: {pid} ---")
        clear_page(pid)
        append_blocks_in_batches(pid, blocks)
        print(f"URL do Notion: https://app.notion.com/p/GDD_Unificado_Favela_Amarela-{pid.replace('-', '')}")
        
    print("\nSincronização com o Notion FINALIZADA com sucesso para todas as páginas!")

if __name__ == '__main__':
    sync_gdd_to_notion()
