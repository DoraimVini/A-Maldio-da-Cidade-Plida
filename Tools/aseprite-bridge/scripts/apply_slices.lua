-- Adiciona Slices ao sprite ativo (não-destrutivo aos pixels; apenas metadata,
-- editável/ajustável depois na UI do Aseprite) e salva no mesmo arquivo.
-- Backup do arquivo original é responsabilidade do chamador (runner.ts), feito
-- ANTES de invocar este script.
--
-- Parâmetro "slices": "nome,x,y,w,h;nome2,x,y,w,h;..."
-- Parâmetro "replace": se "true", remove todas as slices existentes antes de aplicar as novas.

local spr = app.activeSprite
local raw = app.params["slices"] or ""
local replace = app.params["replace"] == "true"

if replace then
  while #spr.slices > 0 do
    spr:deleteSlice(spr.slices[1])
  end
end

local function split(str, sep)
  local parts = {}
  for part in string.gmatch(str, "([^" .. sep .. "]+)") do
    table.insert(parts, part)
  end
  return parts
end

local entries = split(raw, ";")
local created = 0

for _, entry in ipairs(entries) do
  local fields = split(entry, ",")
  if #fields == 5 then
    local name = fields[1]
    local x = tonumber(fields[2])
    local y = tonumber(fields[3])
    local rw = tonumber(fields[4])
    local rh = tonumber(fields[5])

    local slice = spr:newSlice(Rectangle(x, y, rw, rh))
    slice.name = name
    created = created + 1
  end
end

spr:saveAs(spr.filename)
print("CREATED:" .. created)
print("TOTAL_SLICES:" .. #spr.slices)
