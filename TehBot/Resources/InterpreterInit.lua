-- Remove access to import
_G.import = function() end

-- Fix time function
if time then
    local time = time
    _G.time = function(format)
        return time((format ~= nil) and tostring(format) or nil)
    end
end

-- Fix print function
if print then
    local print = print
    _G.print = function(...)
        local args = {...}
        if #args == 0 then
            return
        else
            for i = 1, #args do
                if args[i] == nil then
                    args[i] = "nil"
                end
            end

            print(table.concat(args, "\n"))
        end
    end
end