return function(time, print)
    _G.time = setmetatable({}, {
        __tostring = function(self)
            return time(nil)
        end,

        __call = function(self, format)
            return time((format ~= nil) and tostring(format) or nil)
        end
    })

    if print then
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

                print(table.concat("\n", args))
            end
        end
    end
end