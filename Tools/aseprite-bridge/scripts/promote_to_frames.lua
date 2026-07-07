-- Promove cada Slice existente no sprite (criado antes via apply_slices.lua) a um
-- Frame real, recortando a região correspondente do frame "achatado" original.
-- Ao final, remove o frame achatado original e (opcionalmente) agrupa os frames
-- promovidos em Tags nomeadas.
--
-- Requer que o sprite já tenha Slices (rode aseprite_apply_slices antes).
--
-- Parâmetros:
--   sourceFrame: número do frame achatado a ser recortado e removido (default 1)
--   outputPath:  se vazio, salva no próprio arquivo; senão salva em novo caminho
--   tagGroups:   "nome,deIndice,ateIndice;nome2,deIndice,ateIndice;..." — índices
--                1-based na ORDEM dos slices (== ordem final dos frames promovidos)

local spr = app.activeSprite
local sourceFrame = tonumber(app.params["sourceFrame"]) or 1
local outputPath = app.params["outputPath"] or ""
local tagGroupsRaw = app.params["tagGroups"] or ""

assert(#spr.slices > 0, "Nenhuma Slice encontrada. Rode aseprite_apply_slices antes de promover para frames.")

local flatImg = Image(spr.width, spr.height)
flatImg:drawSprite(spr, sourceFrame)

local layer = spr.layers[1]
local promoted = 0

for _, slice in ipairs(spr.slices) do
  local b = slice.bounds
  local cropped = Image(b.width, b.height)
  cropped:drawImage(flatImg, Point(-b.x, -b.y))

  local newFrame = spr:newEmptyFrame(#spr.frames + 1)
  spr:newCel(layer, newFrame, cropped, Point(0, 0))
  promoted = promoted + 1
end

spr:deleteFrame(sourceFrame)

local function split(str, sep)
  local parts = {}
  for part in string.gmatch(str, "([^" .. sep .. "]+)") do
    table.insert(parts, part)
  end
  return parts
end

local tagCount = 0
if tagGroupsRaw ~= "" then
  for _, entry in ipairs(split(tagGroupsRaw, ";")) do
    local fields = split(entry, ",")
    if #fields == 3 then
      local tag = spr:newTag(tonumber(fields[2]), tonumber(fields[3]))
      tag.name = fields[1]
      tagCount = tagCount + 1
    end
  end
end

if outputPath ~= "" then
  spr:saveAs(outputPath)
else
  spr:saveAs(spr.filename)
end

print("PROMOTED_FRAMES:" .. promoted)
print("TOTAL_FRAMES:" .. #spr.frames)
print("TAGS_CREATED:" .. tagCount)
