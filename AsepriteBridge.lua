
local function applyPalette(colorsJson)
    local sprite = app.activeSprite
    if not sprite then return app.alert("Abra um sprite primeiro!") end
    
    local palette = sprite.palettes[1]
    local colors = json.decode(colorsJson)
    
    palette:resize(#colors)
    for i, hex in ipairs(colors) do
        -- Converter hex para Color do Aseprite
        local r = tonumber(hex:sub(2,3), 16)
        local g = tonumber(hex:sub(4,5), 16)
        local b = tonumber(hex:sub(6,7), 16)
        palette:setColor(i-1, Color{r=r, g=g, b=b, a=255})
    end
end

local dlg = Dialog("Gemini Palette")
dlg:entry{ id="prompt", label="Prompt:", text="Neon Cyberpunk Night" }
dlg:button{ id="gen", text="Gerar Sugestão", onclick=function()
    local prompt = dlg.data.prompt
    app.alert("Enviando prompt ao Claude/MCP: " .. prompt)
    -- Aqui o script avisa que quer uma paleta. 
    -- Como o Lua do Aseprite é isolado, a melhor forma é o usuário copiar o prompt 
    -- ou usarmos um arquivo temporário de sinalização.
end}
dlg:show{ wait=false }
