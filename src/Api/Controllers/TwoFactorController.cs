using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
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
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Bit.Core.Utilities.Duo;
using Bit.Core.Utilities.Hypr;
using Fido2NetLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Controllers
{
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

        public TwoFactorController(
            IUserService userService,
            IOrganizationRepository organizationRepository,
            IOrganizationService organizationService,
            GlobalSettings globalSettings,
            UserManager<User> userManager,
            ICurrentContext currentContext)
        {
            _userService = userService;
            _organizationRepository = organizationRepository;
            _organizationService = organizationService;
            _globalSettings = globalSettings;
            _userManager = userManager;
            _currentContext = currentContext;
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
                duoApi.JSONApiCall<object>("GET", "/auth/v2/check");
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
                duoApi.JSONApiCall<object>("GET", "/auth/v2/check");
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
                if (await _userService.VerifySecretAsync(user, model.Secret))
                {
                    var isBecauseNewDeviceLogin = false;
                    if (user.GetTwoFactorProvider(TwoFactorProviderType.Email) is null
                        &&
                        await _userService.Needs2FABecauseNewDeviceAsync(user, model.DeviceIdentifier, null))
                    {
                        model.ToUser(user);
                        isBecauseNewDeviceLogin = true;
                    }

                    await _userService.SendTwoFactorEmailAsync(user, isBecauseNewDeviceLogin);
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

        [HttpGet("get-device-verification-settings")]
        public async Task<DeviceVerificationResponseModel> GetDeviceVerificationSettings()
        {
            var user = await _userService.GetUserByPrincipalAsync(User);
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (User.Claims.HasSsoIdP())
            {
                return new DeviceVerificationResponseModel(false, false);
            }

            return new DeviceVerificationResponseModel(_userService.CanEditDeviceVerificationSettings(user), user.UnknownDeviceVerificationEnabled);
        }

        [HttpPut("device-verification-settings")]
        public async Task<DeviceVerificationResponseModel> PutDeviceVerificationSettings([FromBody] DeviceVerificationRequestModel model)
        {
            var user = await _userService.GetUserByPrincipalAsync(User);
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (!_userService.CanEditDeviceVerificationSettings(user)
                || User.Claims.HasSsoIdP())
            {
                throw new InvalidOperationException("Can't update device verification settings");
            }

            model.ToUser(user);
            await _userService.SaveUserAsync(user);
            return new DeviceVerificationResponseModel(true, user.UnknownDeviceVerificationEnabled);
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

            try
            {
                string AppID = null;
                var hyprApi = new HyprApi(model.ApiKey, model.ServerURL, AppID);
                var api_response = await hyprApi.ApiCallAsync("GET", "/cc/api/versioned/rpUser");
                if (api_response.StatusCode != HttpStatusCode.OK)
                {
                    throw new BadRequestException("Hypr could not connect.");
                }
            }
            catch (HyprException)
            {
                throw new BadRequestException("Hypr configuration settings are not valid. Please re-check the Hypr Admin panel.");
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
                        mobileBrowser = false
                    }
                };

                using (var hyprApi = new HyprApi(ApiKey, ServerURL, AppID))
                {
                    var jsonMessage = JsonSerializer.Serialize(pushRequestJson);
                    var (httpResponseInitial, pushCallResponse) = await hyprApi.JSONApiCallAsync<HyprAuthResponseJson>("POST", "/rp/api/oob/client/authentication/requests", jsonMessage);

                    if (httpResponseInitial.StatusCode != HttpStatusCode.OK || pushCallResponse is null || pushCallResponse.status.responseCode != 200)
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
                        var (httpResponse, pushGetStatusResponse) = await hyprApi?.JSONApiCallAsync<HyprAuthGetStatusResponseJson>("GET", statusUrl);
                        var states = pushGetStatusResponse?.state;
                        if(states is null)
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
                    };
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

            var response = new HyprAuthenticationResponseModel
            {
                status = 200,
                signature = HyprWeb.SignAuthRequest(_globalSettings.Hypr.SKey, username, teamid)
            };

            return response;
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
    }
}
