package com.abyssaldev.abyss.commands.gateway

import com.abyssaldev.abyss.AbyssEngine
import com.abyssaldev.abyss.framework.common.CommandModule
import com.abyssaldev.abyss.framework.common.reflect.Name
import com.abyssaldev.abyss.framework.gateway.GatewayCommandRequest
import com.abyssaldev.abyss.framework.gateway.reflect.GatewayCommand
import com.abyssaldev.abyss.util.respondSuccess
import com.abyssaldev.abyss.util.write
import net.dv8tion.jda.api.MessageBuilder
import kotlin.reflect.jvm.jvmName

@Name("Admin")
class AdminModule: CommandModule() {
    private val actions = mapOf<String, GatewayCommandRequest.(List<String>) -> String>(
        "exec-db" to {
            "Database not connected."
        },
        "debug-interactions" to {
            "Successful (POST): ${AbyssEngine.instance.interactions.successfulReceivedInteractionRequests}, failed (POST): ${AbyssEngine.instance.interactions.failedReceivedInteractionRequests}"
        },
        "generate-invite" to {
            "<https://discord.com/api/oauth2/authorize?client_id=${AbyssEngine.instance.discordEngine.selfUser.id}&permissions=0&scope=bot%20applications.commands>"
        },
        "dump-interaction-json" to {
            val command = AbyssEngine.instance.interactions.getAllCommands().filter { c ->
                c::class.simpleName == it[0]
            }[0]
            "`${command::class.jvmName}`: ```json\n" + AbyssEngine.jsonEngine.write(command.toJsonMap()) + "```"
        }
    )

    @GatewayCommand(
        name = "admin",
        description = "Provides administrative functions.",
        isOwnerRestricted = true
    )
    fun invoke(call: GatewayCommandRequest): MessageBuilder = respond {
        val actionName = call.args.ordinal(0)?.string
        if (actionName.isNullOrEmpty()) {
            return@respond respondSuccess("Available: `${actions.keys.joinToString(", ")}`")
        }
        val action = actions[actionName]
            ?: return@respond respondSuccess("Available: `${actions.keys.joinToString(", ")}`")

        val startTime = System.currentTimeMillis()
        val response = action.invoke(call, call.args.argSet.drop(1))
        val time = System.currentTimeMillis() - startTime

        val message = "Executed action `${actionName}` in `${time}`ms.\n${response}"
        return@respond respondSuccess(message)
    }
}