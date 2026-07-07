-- Detecta regiões de conteúdo real numa imagem achatada, sem depender de grid fixo.
--
-- Algoritmo em 2 passadas (projeção hierárquica, não flood-fill):
--   1. Projeção de colunas: acha as faixas horizontais que têm pixel não-transparente
--      (separa poses lado a lado).
--   2. Dentro de cada faixa de coluna, projeção de linhas RESTRITA a esse intervalo de
--      X: acha as faixas verticais dentro daquela coluna (separa poses empilhadas na
--      mesma coluna, ex.: variações do mesmo personagem uma embaixo da outra).
--
-- Validado contra Damiao_Clean_Spritesheet.aseprite: a projeção de coluna sozinha
-- juntava até 5 poses empilhadas numa única bbox gigante (banda x=21-74 continha runs
-- verticais separados em 0-87, 173-183, 232-279, 322-375, 392-471). A segunda passada
-- corrige isso sem precisar de flood-fill pixel-a-pixel (mais lento e sensível a poses
-- com partes desconectadas, tipo um braço separado do corpo).

local spr = app.activeSprite
local frameNumber = tonumber(app.params["frame"]) or 1
local minAlpha = tonumber(app.params["minAlpha"]) or 10
local minSize = tonumber(app.params["minSize"]) or 3

local img = Image(spr.width, spr.height)
img:drawSprite(spr, frameNumber)

local w, h = spr.width, spr.height

-- Passada 1: projeção de colunas sobre a imagem inteira.
local colHas = {}
for it in img:pixels() do
  local a = app.pixelColor.rgbaA(it())
  if a and a > minAlpha then
    colHas[it.x] = true
  end
end

local function findRuns(hasTbl, n)
  local result = {}
  local start = nil
  for i = 0, n - 1 do
    if hasTbl[i] and start == nil then
      start = i
    elseif not hasTbl[i] and start ~= nil then
      table.insert(result, { start, i - 1 })
      start = nil
    end
  end
  if start ~= nil then table.insert(result, { start, n - 1 }) end
  return result
end

local columnBands = findRuns(colHas, w)

-- Passada 2: para cada banda de coluna, projeção de linhas restrita a esse X, e bbox
-- horizontal justa dentro de cada faixa de linha resultante.
local regionIndex = 0

for _, band in ipairs(columnBands) do
  local bandFromX, bandToX = band[1], band[2]

  local rowHas = {}
  local rowMinX = {}
  local rowMaxX = {}
  for y = 0, h - 1 do
    for x = bandFromX, bandToX do
      local a = app.pixelColor.rgbaA(img:getPixel(x, y))
      if a and a > minAlpha then
        rowHas[y] = true
        if rowMinX[y] == nil or x < rowMinX[y] then rowMinX[y] = x end
        if rowMaxX[y] == nil or x > rowMaxX[y] then rowMaxX[y] = x end
      end
    end
  end

  local rowRuns = findRuns(rowHas, h)

  for _, run in ipairs(rowRuns) do
    local fromY, toY = run[1], run[2]

    local minX, maxX = nil, nil
    for y = fromY, toY do
      if rowMinX[y] ~= nil then
        if minX == nil or rowMinX[y] < minX then minX = rowMinX[y] end
        if maxX == nil or rowMaxX[y] > maxX then maxX = rowMaxX[y] end
      end
    end
    if minX == nil then goto continue end

    local rw = maxX - minX + 1
    local rh = toY - fromY + 1
    if rw < minSize or rh < minSize then goto continue end

    regionIndex = regionIndex + 1
    print("REGION:" .. regionIndex .. ":" .. minX .. ":" .. fromY .. ":" .. rw .. ":" .. rh)

    ::continue::
  end
end

print("TOTAL:" .. regionIndex)
