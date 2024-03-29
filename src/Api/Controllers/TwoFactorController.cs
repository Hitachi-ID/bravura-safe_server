﻿using System.Net;
using System.Text.Json;
using Bit.Api.Models.Request;
using Bit.Api.Models.Request.Accounts;
using Bit.Api.Models.Request.Hypr;
using Bit.Api.Models.Response;
using Bit.Api.Models.Response.Hypr;
using Bit.Api.Models.Response.TwoFactor;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.LoginFeatures.PasswordlessLogin.Interfaces;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Bit.Core.Utilities.Hypr;
using Fido2NetLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bit.Core.Models.Api.Error.Hypr;

namespace Bit.Api.Controllers;

[Route("two-factor")]
[Authorize("Web")]
public class TwoFactorController : Controller
{
    private readonly IUserService _userService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationService _organizationService;
    private readonly GlobalSettings _globalSettings;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentContext _currentContext;
    private readonly IVerifyAuthRequestCommand _verifyAuthRequestCommand;

    public TwoFactorController(
        IUserService userService,
        IOrganizationRepository organizationRepository,
        IOrganizationService organizationService,
        GlobalSettings globalSettings,
        UserManager<User> userManager,
        ICurrentContext currentContext,
        IVerifyAuthRequestCommand verifyAuthRequestCommand)
    {
        _userService = userService;
        _organizationRepository = organizationRepository;
        _organizationService = organizationService;
        _globalSettings = globalSettings;
        _userManager = userManager;
        _currentContext = currentContext;
        _verifyAuthRequestCommand = verifyAuthRequestCommand;
    }

    [HttpGet("")]
    public async Task<ListResponseModel<TwoFactorProviderResponseModel>> Get()
    {
        var user = await _userService.GetUserByPrincipalAsync(User);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        var providers = user.GetTwoFactorProviders()?.Select(
            p => new TwoFactorProviderResponseModel(p.Key, p.Value));
        return new ListResponseModel<TwoFactorProviderResponseModel>(providers);
    }

    [HttpGet("~/organizations/{id}/two-factor")]
    public async Task<ListResponseModel<TwoFactorProviderResponseModel>> GetOrganization(string id)
    {
        var orgIdGuid = new Guid(id);
        if (!await _currentContext.OrganizationAdmin(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        var providers = organization.GetTwoFactorProviders()?.Select(
            p => new TwoFactorProviderResponseModel(p.Key, p.Value));
        return new ListResponseModel<TwoFactorProviderResponseModel>(providers);
    }

    [HttpGet("~/organizations/{id}/two-factor-enabled/{twoFactorType}")]
    public async Task<bool> GetOrganization(string id, TwoFactorProviderType twoFactorType)
    {
        var orgIdGuid = new Guid(id);

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        var provider = organization.GetTwoFactorProvider(twoFactorType);
        return provider.Enabled;
    }

    [HttpPost("get-authenticator")]
    public async Task<TwoFactorAuthenticatorResponseModel> GetAuthenticator([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, false);
        var response = new TwoFactorAuthenticatorResponseModel(user);
        return response;
    }

    [HttpPut("authenticator")]
    [HttpPost("authenticator")]
    public async Task<TwoFactorAuthenticatorResponseModel> PutAuthenticator(
        [FromBody] UpdateTwoFactorAuthenticatorRequestModel model)
    {
        var user = await CheckAsync(model, false);
        model.ToUser(user);

        if (!await _userManager.VerifyTwoFactorTokenAsync(user,
            CoreHelpers.CustomProviderName(TwoFactorProviderType.Authenticator), model.Token))
        {
            await Task.Delay(2000);
            throw new BadRequestException("Token", "Invalid token.");
        }

        await _userService.UpdateTwoFactorProviderAsync(user, TwoFactorProviderType.Authenticator);
        var response = new TwoFactorAuthenticatorResponseModel(user);
        return response;
    }

    [HttpPost("get-yubikey")]
    public async Task<TwoFactorYubiKeyResponseModel> GetYubiKey([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, true);
        var response = new TwoFactorYubiKeyResponseModel(user);
        return response;
    }

    [HttpPut("yubikey")]
    [HttpPost("yubikey")]
    public async Task<TwoFactorYubiKeyResponseModel> PutYubiKey([FromBody] UpdateTwoFactorYubicoOtpRequestModel model)
    {
        var user = await CheckAsync(model, true);
        model.ToUser(user);

        await ValidateYubiKeyAsync(user, nameof(model.Key1), model.Key1);
        await ValidateYubiKeyAsync(user, nameof(model.Key2), model.Key2);
        await ValidateYubiKeyAsync(user, nameof(model.Key3), model.Key3);
        await ValidateYubiKeyAsync(user, nameof(model.Key4), model.Key4);
        await ValidateYubiKeyAsync(user, nameof(model.Key5), model.Key5);

        await _userService.UpdateTwoFactorProviderAsync(user, TwoFactorProviderType.YubiKey);
        var response = new TwoFactorYubiKeyResponseModel(user);
        return response;
    }

    [HttpPost("get-duo")]
    public async Task<TwoFactorDuoResponseModel> GetDuo([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, true);
        var response = new TwoFactorDuoResponseModel(user);
        return response;
    }

    [HttpPut("duo")]
    [HttpPost("duo")]
    public async Task<TwoFactorDuoResponseModel> PutDuo([FromBody] UpdateTwoFactorDuoRequestModel model)
    {
        var user = await CheckAsync(model, true);
        try
        {
            var duoApi = new DuoApi(model.IntegrationKey, model.SecretKey, model.Host);
            await duoApi.JSONApiCall("GET", "/auth/v2/check");
        }
        catch (DuoException)
        {
            throw new BadRequestException("Duo configuration settings are not valid. Please re-check the Duo Admin panel.");
        }

        model.ToUser(user);
        await _userService.UpdateTwoFactorProviderAsync(user, TwoFactorProviderType.Duo);
        var response = new TwoFactorDuoResponseModel(user);
        return response;
    }

    [HttpPost("~/organizations/{id}/two-factor/get-duo")]
    public async Task<TwoFactorDuoResponseModel> GetOrganizationDuo(string id,
        [FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        var response = new TwoFactorDuoResponseModel(organization);
        return response;
    }

    [HttpPut("~/organizations/{id}/two-factor/duo")]
    [HttpPost("~/organizations/{id}/two-factor/duo")]
    public async Task<TwoFactorDuoResponseModel> PutOrganizationDuo(string id,
        [FromBody] UpdateTwoFactorDuoRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        try
        {
            var duoApi = new DuoApi(model.IntegrationKey, model.SecretKey, model.Host);
            await duoApi.JSONApiCall("GET", "/auth/v2/check");
        }
        catch (DuoException)
        {
            throw new BadRequestException("Duo configuration settings are not valid. Please re-check the Duo Admin panel.");
        }

        model.ToOrganization(organization);
        await _organizationService.UpdateTwoFactorProviderAsync(organization,
            TwoFactorProviderType.OrganizationDuo);
        var response = new TwoFactorDuoResponseModel(organization);
        return response;
    }

    [HttpPost("get-webauthn")]
    public async Task<TwoFactorWebAuthnResponseModel> GetWebAuthn([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, true);
        var response = new TwoFactorWebAuthnResponseModel(user);
        return response;
    }

    [HttpPost("get-webauthn-challenge")]
    [ApiExplorerSettings(IgnoreApi = true)] // Disable Swagger due to CredentialCreateOptions not converting properly
    public async Task<CredentialCreateOptions> GetWebAuthnChallenge([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, true);
        var reg = await _userService.StartWebAuthnRegistrationAsync(user);
        return reg;
    }

    [HttpPut("webauthn")]
    [HttpPost("webauthn")]
    public async Task<TwoFactorWebAuthnResponseModel> PutWebAuthn([FromBody] TwoFactorWebAuthnRequestModel model)
    {
        var user = await CheckAsync(model, true);

        var success = await _userService.CompleteWebAuthRegistrationAsync(
            user, model.Id.Value, model.Name, model.DeviceResponse);
        if (!success)
        {
            throw new BadRequestException("Unable to complete WebAuthn registration.");
        }
        var response = new TwoFactorWebAuthnResponseModel(user);
        return response;
    }

    [HttpDelete("webauthn")]
    public async Task<TwoFactorWebAuthnResponseModel> DeleteWebAuthn([FromBody] TwoFactorWebAuthnDeleteRequestModel model)
    {
        var user = await CheckAsync(model, true);
        await _userService.DeleteWebAuthnKeyAsync(user, model.Id.Value);
        var response = new TwoFactorWebAuthnResponseModel(user);
        return response;
    }

    [HttpPost("get-email")]
    public async Task<TwoFactorEmailResponseModel> GetEmail([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, false);
        var response = new TwoFactorEmailResponseModel(user);
        return response;
    }

    [HttpPost("send-email")]
    public async Task SendEmail([FromBody] TwoFactorEmailRequestModel model)
    {
        var user = await CheckAsync(model, false);
        model.ToUser(user);
        await _userService.SendTwoFactorEmailAsync(user);
    }

    [AllowAnonymous]
    [HttpPost("send-email-login")]
    public async Task SendEmailLogin([FromBody] TwoFactorEmailRequestModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email.ToLowerInvariant());
        if (user != null)
        {
            // check if 2FA email is from passwordless
            if (!string.IsNullOrEmpty(model.AuthRequestAccessCode))
            {
                if (await _verifyAuthRequestCommand
                        .VerifyAuthRequestAsync(new Guid(model.AuthRequestId), model.AuthRequestAccessCode))
                {
                    await _userService.SendTwoFactorEmailAsync(user);
                    return;
                }
            }
            else if (await _userService.VerifySecretAsync(user, model.Secret))
            {
                await _userService.SendTwoFactorEmailAsync(user);
                    return;
                }
            }

        await Task.Delay(2000);
        throw new BadRequestException("Cannot send two-factor email.");
    }

    [HttpPut("email")]
    [HttpPost("email")]
    public async Task<TwoFactorEmailResponseModel> PutEmail([FromBody] UpdateTwoFactorEmailRequestModel model)
    {
        var user = await CheckAsync(model, false);
        model.ToUser(user);

        if (!await _userManager.VerifyTwoFactorTokenAsync(user,
            CoreHelpers.CustomProviderName(TwoFactorProviderType.Email), model.Token))
        {
            await Task.Delay(2000);
            throw new BadRequestException("Token", "Invalid token.");
        }

        await _userService.UpdateTwoFactorProviderAsync(user, TwoFactorProviderType.Email);
        var response = new TwoFactorEmailResponseModel(user);
        return response;
    }

    [HttpPut("disable")]
    [HttpPost("disable")]
    public async Task<TwoFactorProviderResponseModel> PutDisable([FromBody] TwoFactorProviderRequestModel model)
    {
        var user = await CheckAsync(model, false);
        await _userService.DisableTwoFactorProviderAsync(user, model.Type.Value, _organizationService);
        var response = new TwoFactorProviderResponseModel(model.Type.Value, user);
        return response;
    }

    [HttpPut("~/organizations/{id}/two-factor/disable")]
    [HttpPost("~/organizations/{id}/two-factor/disable")]
    public async Task<TwoFactorProviderResponseModel> PutOrganizationDisable(string id,
        [FromBody] TwoFactorProviderRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        await _organizationService.DisableTwoFactorProviderAsync(organization, model.Type.Value);
        var response = new TwoFactorProviderResponseModel(model.Type.Value, organization);
        return response;
    }

    [HttpPut("~/organizations/{id}/two-factor/delete")]
    [HttpPost("~/organizations/{id}/two-factor/delete")]
    public async Task<TwoFactorProviderResponseModel> PutOrganizationDelete(string id,
        [FromBody] TwoFactorProviderRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        await _organizationService.DeleteTwoFactorProviderAsync(organization, model.Type.Value);
        var response = new TwoFactorProviderResponseModel(model.Type.Value, organization);
        return response;
    }

    [HttpPost("get-recover")]
    public async Task<TwoFactorRecoverResponseModel> GetRecover([FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, false);
        var response = new TwoFactorRecoverResponseModel(user);
        return response;
    }

    [HttpPost("recover")]
    [AllowAnonymous]
    public async Task PostRecover([FromBody] TwoFactorRecoveryRequestModel model)
    {
        if (!await _userService.RecoverTwoFactorAsync(model.Email, model.MasterPasswordHash, model.RecoveryCode,
            _organizationService))
        {
            await Task.Delay(2000);
            throw new BadRequestException(string.Empty, "Invalid information. Try again.");
        }
    }

    [Obsolete("Leaving this for backwards compatibilty on clients")]
    [HttpGet("get-device-verification-settings")]
    public Task<DeviceVerificationResponseModel> GetDeviceVerificationSettings()
    {
        return Task.FromResult(new DeviceVerificationResponseModel(false, false));
        }

    [Obsolete("Leaving this for backwards compatibilty on clients")]
    [HttpPut("device-verification-settings")]
    public Task<DeviceVerificationResponseModel> PutDeviceVerificationSettings([FromBody] DeviceVerificationRequestModel model)
    {
        return Task.FromResult(new DeviceVerificationResponseModel(false, false));
        }

    [HttpPost("~/organizations/{id}/two-factor/get-hypr")]
    public async Task<TwoFactorHyprResponseModel> GetOrganizationHypr(string id, [FromBody] SecretVerificationRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        var response = new TwoFactorHyprResponseModel(organization);
        return response;
    }

    [HttpPut("~/organizations/{id}/two-factor/hypr")]
    [HttpPost("~/organizations/{id}/two-factor/hypr")]
    public async Task<TwoFactorHyprResponseModel> PutOrganizationHypr(string id,
        [FromBody] UpdateTwoFactorHyprRequestModel model)
    {
        var user = await CheckAsync(model, false);

        var orgIdGuid = new Guid(id);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
        if (organization == null)
        {
            throw new NotFoundException();
        }

        var sKey = _globalSettings.Hypr.SKey;

        if (String.IsNullOrEmpty(sKey))
        {
            throw new BadRequestException("Hypr signature key is not set.");
        }

        try
        {
            var pushRequestJson = new HyprAuthRequestJson
            {
                actionId = "defaultAuthAction",
                appId = model.AppId,
                machineId = "push-notif-request",
                namedUser = "NhXDJZyW2il1FeUUrCUJ@2KfBjrSL2Unk8LXEjgbM",
                machine = "WEB",
                sessionNonce = HyprApi.GetNonce(),
                deviceNonce = HyprApi.GetNonce(),
                serviceNonce = HyprApi.GetNonce(),
                serviceHmac = HyprApi.GetNonce(),
                additionalDetails = new HyprAuthRequestJsonAdditionalDetails()
                {
                    mobileBrowser = false
                }
            };

            using (var hyprApi = new HyprApi(model.ApiKey, model.ServerURL, model.AppId))
            {
                var jsonMessage = JsonSerializer.Serialize(pushRequestJson);
                var (httpResponseInitial, pushCallResponse) = await hyprApi.JSONApiCallAsync<HyprAuthResponseJson>("POST", "/rp/api/oob/client/authentication/requests", jsonMessage);

                if (httpResponseInitial.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new HyprException("API Token is invalid.");
                }
                else if (httpResponseInitial.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HyprException("Application ID is invalid.");
                }
                else if (httpResponseInitial.StatusCode != HttpStatusCode.BadRequest)
                {
                    throw new HyprException("Push notification failed to initiate.");
                }
            }
        }
        catch(System.Net.Http.HttpRequestException)
        {
            throw new BadRequestException("Hypr configuration settings are not valid. Server Address is invalid.");
        }
        catch (HyprException e)
        {
            throw new BadRequestException("Hypr configuration settings are not valid. " + e.Message);
        }

        model.ToOrganization(organization);
        await _organizationService.UpdateTwoFactorProviderAsync(organization,
            TwoFactorProviderType.OrganizationHypr);
        var response = new TwoFactorHyprResponseModel(organization);
        return response;
    }

    [HttpPost("hypr/push-authentication")]
    [AllowAnonymous]
    public async Task<HyprAuthenticationResponseModel> OrganizationHyprRequestPushAuthentication([FromBody] HyprAuthenticationRequestModel model)
    {
        var (username, teamid) = HyprWeb.VerifyTxResponse(_globalSettings.Hypr.SKey, model.Signature);
        var teamguid = new Guid(teamid);
        var team = await _organizationRepository.GetByIdAsync(teamguid);
        if (team == null)
        {
            throw new NotFoundException();
        }

        string ServerURL = null;
        string ApiKey = null;
        string AppID = null;
        var provider = team.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
        if (provider.MetaData.ContainsKey("Server"))
        {
            ServerURL = (string)provider.MetaData["Server"];
        }
        if (provider.MetaData.ContainsKey("AKey"))
        {
            ApiKey = (string)provider.MetaData["AKey"];
        }
        if (provider.MetaData.ContainsKey("App"))
        {
            AppID = (string)provider.MetaData["App"];
        }
        if (ApiKey is null || ServerURL is null || AppID is null)
        {
            throw new BadRequestException("Hypr not configured. Check two-factor login settings.");
        }

        bool stop = false, found = false;
        try
        {
            var pushRequestJson = new HyprAuthRequestJson
            {
                actionId = "defaultAuthAction",
                appId = AppID,
                machineId = "push-notif-request",
                namedUser = username,
                machine = "WEB",
                sessionNonce = HyprApi.GetNonce(),
                deviceNonce = HyprApi.GetNonce(),
                serviceNonce = HyprApi.GetNonce(),
                serviceHmac = HyprApi.GetNonce(),
                additionalDetails =  new HyprAuthRequestJsonAdditionalDetails()
                {
                    mobileBrowser = model.MobileBrowser
                }
            };

            using (var hyprApi = new HyprApi(ApiKey, ServerURL, AppID))
            {
                var jsonMessage = JsonSerializer.Serialize(pushRequestJson);
                var (httpResponseInitial, respObj) = await hyprApi.JSONApiCallAsync<HyprAuthResponseJson>("POST", "/rp/api/oob/client/authentication/requests", jsonMessage);

                if (respObj is HyprAuthResponseJson pushCallResponse)
                {
                    if (httpResponseInitial.StatusCode != HttpStatusCode.OK || pushCallResponse.status.responseCode != 200)
                    {
                        if (httpResponseInitial is not null)
                        {
                            string jsonResultStr = httpResponseInitial.Content.ReadAsStringAsync().Result;
                            Dictionary<string, object> result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResultStr);
                            throw new HyprException(result["title"].ToString());
                        }
                        else
                        {
                            throw new HyprException("Push notification failed to initiate.");
                        }
                    }

                    var requestid = pushCallResponse.response.requestId;
                    var requestResponse = pushCallResponse.status.responseCode;
                    string statusUrl = "/rp/api/oob/client/authentication/requests/" + requestid;
                    int pollTime = 250;

                    if (requestResponse == 200 && string.IsNullOrWhiteSpace(requestid))
                    {
                        throw new HyprException("Hypr authentication request returned is not valid.");
                    }

                    while (!stop && !found)
                    {
                        var (httpResponse, respObject) = await hyprApi?.JSONApiCallAsync<HyprAuthGetStatusResponseJson>("GET", statusUrl);
                        if(respObject is HyprErrorResponseJson errResponse)
                        {
                            found = false;
                            stop = true;
                            return ReturnHyprResponse(400, errResponse.title, errResponse.errorCode);
                        }
                        else if (respObject is HyprAuthGetStatusResponseJson pushGetStatusResponse)
                        {
                            var states = pushGetStatusResponse?.state;
                            if (states is null)
                            {
                                throw new HyprException("No request found.");
                            }
                            else
                            {
                                found = states.Any(item => item.value == "COMPLETED");
                                stop = states.Any(item => item.value == "CANCELED" || item.value == "FAILED") || httpResponse.StatusCode != HttpStatusCode.OK || hyprApi is null;
                            }

                            if (!stop && !found)
                            {
                                await Task.Delay(pollTime);
                            }
                        }
                    };
                }
                else if (respObj is HyprErrorResponseJson errResponse)
                {
                    return ReturnHyprResponse(400, errResponse.title, errResponse.errorCode);
                }
                else
                {
                    throw new HyprException("Push notification failed to initiate.");
                }
            }

        }
        catch (HyprException e)
        {
            throw new BadRequestException(e.Message);
        }

        if (!found || stop)
        {
            throw new UnauthorizedAccessException();
        }

        return ReturnHyprResponse(200, "", 0, HyprWeb.SignAuthRequest(_globalSettings.Hypr.SKey, username, teamid));
    }

    [HttpPost("hypr/mail-registration")]
    [AllowAnonymous]
    public async Task<HyprAuthenticationResponseModel> OrganizationHyprSendEmailRegistration([FromBody] HyprAuthenticationRequestModel model)
    {
        var (username, teamid) = HyprWeb.VerifyTxResponse(_globalSettings.Hypr.SKey, model.Signature);

        var (url, user, secondsValid) = await GetMagicLinkHypr(model, username);
        if (!string.IsNullOrWhiteSpace(url))
        {
            var linkExpiration = DateTime.Now.AddSeconds(secondsValid);
            await _userService.SendHyprMagicLinkEmailAsync(user, url, linkExpiration);
            return ReturnHyprResponse(200, "Email dispatched.");
        }

        return ReturnHyprResponse(400, "Email not dispatched.");
    }

    [HttpPost("hypr/goto-hypr-management")]
    public async Task<HyprAuthGetMagicLink> OrganizationHyprManage([FromBody] HyprAuthenticationRequestModel model)
    {
        var thisuser = await _userService.GetUserByPrincipalAsync(User);
        var (url, user, secondsValid) = await GetMagicLinkHypr(model, thisuser.Email);
        return new HyprAuthGetMagicLink
        {
            url = url,
            message = "",
            status = 200
        };
    }

    private async Task<(string, User, int)> GetMagicLinkHypr(HyprAuthenticationRequestModel model, string username)
    {
        string url = "";
        var teamguid = new Guid(model.Team);
        var team = await _organizationRepository.GetByIdAsync(teamguid);
        if (team == null)
        {
            throw new NotFoundException();
        }
        var user = await _userManager.FindByEmailAsync(username.ToLowerInvariant());
        int secondsValid = 3600;

        string ServerURL = null;
        string ApiKey = null;
        string AppID = null;
        var provider = team.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
        if (provider.MetaData.ContainsKey("Server"))
        {
            ServerURL = (string)provider.MetaData["Server"];
        }
        if (provider.MetaData.ContainsKey("AKey"))
        {
            ApiKey = (string)provider.MetaData["AKey"];
        }
        if (provider.MetaData.ContainsKey("App"))
        {
            AppID = (string)provider.MetaData["App"];
        }
        if (ApiKey is null || ServerURL is null || AppID is null)
        {
            throw new BadRequestException("Hypr not configured. Check two-factor login settings.");
        }
        if (provider.MetaData.ContainsKey("LinkExpires"))
        {
            secondsValid = Convert.ToInt32(provider.MetaData["LinkExpires"]);
        }

        try
        {
            var pushRequestJson = new HyprMagicLinkRequestModel
            {

                username = username,
                message = "Bravura Safe OneAuth Magic Link request generation",
                secondsValid = secondsValid,
                mobileDeepLinkPrefix = "hypr://register",
                hyprServerUrl = "https://" + ServerURL,
                registrationsLimit = 1
            };

            using (var hyprApi = new HyprApi(ApiKey, ServerURL, AppID))
            {
                var jsonMessage = JsonSerializer.Serialize(pushRequestJson);
                var (httpResponseInitial, respObj) = await hyprApi.JSONApiCallAsync<HyprMagicLinkResponseModel>("POST", "/rp/api/versioned/magiclink", jsonMessage);

                if (httpResponseInitial.StatusCode != HttpStatusCode.OK || respObj is HyprErrorResponseJson)
                {
                    if (httpResponseInitial is not null)
                    {
                        string jsonResultStr = httpResponseInitial.Content.ReadAsStringAsync().Result;
                        Dictionary<string, object> result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResultStr);
                        throw new HyprException(result["title"].ToString());
                    }
                    else
                    {
                        throw new HyprException("Push notification failed to initiate.");
                    }
                }
                else if (respObj is HyprMagicLinkResponseModel pushCallResponse)
                {
                    url = pushCallResponse.firebaseDynamicLinkForHyprApp;
                }
            }

        }
        catch (HyprException e)
        {
            throw new BadRequestException(e.Message);
        }

        return (url, user, secondsValid);
    }

        private async Task<User> CheckAsync(SecretVerificationRequestModel model, bool premium)
    {
        var user = await _userService.GetUserByPrincipalAsync(User);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        if (!await _userService.VerifySecretAsync(user, model.Secret))
        {
            await Task.Delay(2000);
            throw new BadRequestException(string.Empty, "User verification failed.");
        }

        if (premium && !(await _userService.CanAccessPremium(user)))
        {
            throw new BadRequestException("Premium status is required.");
        }

        return user;
    }

    private async Task ValidateYubiKeyAsync(User user, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length == 12)
        {
            return;
        }

        if (!await _userManager.VerifyTwoFactorTokenAsync(user,
            CoreHelpers.CustomProviderName(TwoFactorProviderType.YubiKey), value))
        {
            await Task.Delay(2000);
            throw new BadRequestException(name, $"{name} is invalid.");
        }
        else
        {
            await Task.Delay(500);
        }
    }

    private HyprAuthenticationResponseModel ReturnHyprResponse(int status, string message, int errorCode = 0, string signature = null)
    { 
        var err = new HyprAuthenticationResponseModel
        {
            status = status,
            errorCode = errorCode,
            message = message,
            signature = signature
        };
        return err;
    }
}
