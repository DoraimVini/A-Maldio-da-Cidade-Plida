-- Imprime metadados do sprite ativo em linhas "CHAVE:valor" para o Node fazer parse.
local spr = app.activeSprite

print("WIDTH:" .. spr.width)
print("HEIGHT:" .. spr.height)
print("FRAMES:" .. #spr.frames)

for _, layer in ipairs(spr.layers) do
  print("LAYER:" .. layer.name)
end

for _, tag in ipairs(spr.tags) do
  print("TAG:" .. tag.name .. ":" .. tag.fromFrame.frameNumber .. ":" .. tag.toFrame.frameNumber)
end

for _, slice in ipairs(spr.slices) do
  local b = slice.bounds
  print("SLICE:" .. slice.name .. ":" .. b.x .. ":" .. b.y .. ":" .. b.width .. ":" .. b.height)
end
