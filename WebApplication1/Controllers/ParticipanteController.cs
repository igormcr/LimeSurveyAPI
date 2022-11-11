using Newtonsoft.Json.Linq;
using PesquisaWebApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ParticipanteController : ApiController
    {
        public bool execMetodo = false;
        /// <summary>
        /// Endpoint responsável por adicionar o participante
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/AdicionarParticipante")]
        public string AdicionarParticipante([FromBody] Participante model)
        {
            string Baseurl = ConfigurationManager.AppSettings["apiUrl"];
            JsonRPCclient client = new JsonRPCclient(Baseurl);
            client.Method = "get_session_key";
            client.Parameters.Add("username", ConfigurationManager.AppSettings["apiUsername"]);
            client.Parameters.Add("password", ConfigurationManager.AppSettings["apiPassword"]);
            client.Post();
            string SessionKey = client.Response.result.ToString();
            string msg = "";
            client.ClearParameters();

            var nomes = model.nomeParticipante;

            var split = model.nomeParticipante.Split(new char[] { ' ' }, nomes.IndexOf(" ") + 1);

            if (nomes.Length < 1 || string.IsNullOrEmpty(nomes))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Por favor verificar o nome", System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.BadRequest
                };
                client.ClearParameters();
                throw new HttpResponseException(resp);
            }

            if (split?.Length > 3)
            {
                model.ultimoNome = split[2] + " " + split[3];
                model.primeiroNome = split[0];
            }
            else
            {
                model.ultimoNome = split[1];
                model.primeiroNome = split[0];
            }

            if (SessionKey != null)
            {
                dynamic user = new JObject();
                object email = null;
                object firstname = nomes[0];
                object lastname = nomes[1];
                object token = null;
                object uid = null;
                object txtiSurveyID = null;
                object cd_externo = null;

                user.txtiSurveyID = model.id_pesquisa;
                user.email = model.emailParticipante;
                user.lastname = model.ultimoNome;
                user.firstname = model.primeiroNome;
                user.txtiSurveyID = model.id_pesquisa;
                user.token = SessionKey;
                user.cd_externo = model.cd_chave_externo;

                if (VerificarParticipante(model.cd_chave_externo, model.id_pesquisa) == true)
                {
                    JObject UpdateAccProfile = new JObject(
                    new JProperty("user", user));
                    var _json_user = JObject.Parse(UpdateAccProfile.ToString());

                    client.Method = "add_participants";
                    client.Parameters.Add("sSessionKey", Convert.ToString(SessionKey));
                    client.Parameters.Add("iSurveyID", user.txtiSurveyID);
                    client.Parameters.Add("aParticipantData", _json_user);
                    client.Parameters.Add("bCreateToken", 0);
                    var _ret = client.Post();
                    client.Post();


                    if (string.IsNullOrEmpty(model.id_envio_pesquisa) || string.IsNullOrEmpty(model.emailParticipante))
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Erro, verificar informações do participante" + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.BadRequest
                        };
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }

                    if (!string.IsNullOrEmpty(_ret) || (_ret.Length > 90))
                    {

                        var idres = _ret.Split(new[] { "tid" }, StringSplitOptions.None)[1];
                        idres = idres.Substring(4, 3);
                        model.id_usuario_retorno = idres;



                        tbl_pesquisa_usuario _participante = new tbl_pesquisa_usuario();
                        _participante.cd_chave_externo = model.cd_chave_externo;

                        _participante.id_pesquisa = Convert.ToInt32(model.id_pesquisa);

                        _participante.id_envio_pesquisa = Convert.ToInt32(model.id_envio_pesquisa);

                        _participante.id_usuario_retorno = Convert.ToString(idres);

                        _participante.id_token_retorno = SessionKey;

                        _participante.dt_cadastro = DateTime.Now;

                        SalvarParticipante(model, _participante);
                        var resp = new HttpResponseMessage(HttpStatusCode.Created)
                        {
                            Content = new StringContent("Participante Adicionado" + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.Created
                        };
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }
                    else
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Erro, verificar informações de usuário, objeto nulo.  " + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.BadRequest
                        };
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }
                }
                else
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                    {
                        Content = new StringContent("Erro, usuário já existente  ", System.Text.Encoding.UTF8, "text/plain"),
                        StatusCode = HttpStatusCode.MethodNotAllowed
                    };
                    client.ClearParameters();
                    throw new HttpResponseException(resp);

                }
            }
            var res = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Erro, token inválido  ", System.Text.Encoding.UTF8, "text/plain"),
                StatusCode = HttpStatusCode.BadRequest
            };
            client.ClearParameters();
            throw new HttpResponseException(res);
        }
        /// <summary>
        /// Endpoint para o convite de usuário para pesquisa.
        /// ele é ativado apenas com ID da pesquisa, ele dispara todos os email's com token de disparo.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/ConvidarParticipante")]
        public string ConvidarParticipante([FromBody] Participante model)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.BadRequest);
            string Baseurl = ConfigurationManager.AppSettings["apiUrl"];
            JsonRPCclient client = new JsonRPCclient(Baseurl);
            client.Method = "get_session_key";
            client.Parameters.Add("username", ConfigurationManager.AppSettings["apiUsername"]);
            client.Parameters.Add("password", ConfigurationManager.AppSettings["apiPassword"]);
            client.Post();
            string SessionKey = client.Response.result.ToString();
            string msg = "";
            client.ClearParameters();

            if (SessionKey != null)
            {



                dynamic user = new JObject();
                object token = null;
                object uid = null;
                object txtiSurveyID = null;
                object emailstatus = "Ok";

                user.token = model.cd_chave_externo;
                user.uid = model.id_usuario_retorno;

                JObject UpdateAccProfile = new JObject(
                new JProperty("user", user));
                var _json_user = JObject.Parse(UpdateAccProfile.ToString());
                client.ClearParameters();

                client.Method = "invite_participants";
                //CHAVES DO METODO invite_participants(string $sSessionKey,integer $iSurveyID,array $aTokenIDs = null,boolean $bEmail = true): array
                client.Parameters.Add("sSessionKey", Convert.ToString(SessionKey));
                client.Parameters.Add("iSurveyID", Convert.ToInt32(model.id_pesquisa));
                client.Parameters.Add("aTokenIDs", model.cd_chave_externo);
                client.Parameters.Add("bEmail", true);
                client.Parameters.Add("bCreateToken", 0);

                var _ret = client.Post();
                client.Post();

                if ((_ret.Length < 110))
                {
                    resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Resposta muita curta, ação pode não ter sido concluida,"
                        +"a ação só pode ser realizada se os participantes ainda tiverem tokens para serem enviados.  " 
                        + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                        StatusCode = HttpStatusCode.Accepted
                    };
                    client.ClearParameters();
                    throw new HttpResponseException(resp);
                }


                resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Convite enviado : " 
                    + Environment.NewLine 
                    + _ret, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.OK
                };
                client.ClearParameters();
                throw new HttpResponseException(resp);
            }
            resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Erro, token da sessão invalido",
                System.Text.Encoding.UTF8, "text/plain"),
                StatusCode = HttpStatusCode.BadRequest
            };
            client.ClearParameters();
            throw new HttpResponseException(resp);

        }

        /// <summary>
        /// Endpoint para adicionar e convidar participantes de uma vez só
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/AdicionarConvidarParticipante")]
        public string AdicionarConvidarParticipante([FromBody] Participante model)
        {
            bool exeMtds = true;

            AdicionarParticipante(model, exeMtds);
            ConvidarParticipante(model);

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Participante Adicionado e Convidado.",
                System.Text.Encoding.UTF8, "text/plain"),
                StatusCode = HttpStatusCode.OK
            };
            throw new HttpResponseException(resp);
        }




        [HttpPost]
        public string AdicionarParticipante([FromBody] Participante model, bool exeMtds)
        {
            string Baseurl = ConfigurationManager.AppSettings["apiUrl"];
            JsonRPCclient client = new JsonRPCclient(Baseurl);
            client.Method = "get_session_key";
            client.Parameters.Add("username", ConfigurationManager.AppSettings["apiUsername"]);
            client.Parameters.Add("password", ConfigurationManager.AppSettings["apiPassword"]);
            client.Post();
            string SessionKey = client.Response.result.ToString();
            string msg = "";
            client.ClearParameters();

            var nomes = model.nomeParticipante;

            var split = model.nomeParticipante.Split(new char[] { ' ' }, nomes.IndexOf(" ") + 1);

            if (nomes.Length < 1 || string.IsNullOrEmpty(nomes))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Por favor verificar o nome", System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.BadRequest
                };
                client.ClearParameters();
                throw new HttpResponseException(resp);
            }

            if (split?.Length > 3)
            {
                model.ultimoNome = split[2] + " " + split[3];
                model.primeiroNome = split[0];
            }
            else
            {
                model.ultimoNome = split[1];
                model.primeiroNome = split[0];
            }

            if (SessionKey != null)
            {
                dynamic user = new JObject();
                object email = null;
                object firstname = nomes[0];
                object lastname = nomes[1];
                object token = null;
                object uid = null;
                object txtiSurveyID = null;
                object cd_externo = null;

                user.txtiSurveyID = model.id_pesquisa;
                user.email = model.emailParticipante;
                user.lastname = model.ultimoNome;
                user.firstname = model.primeiroNome;
                user.txtiSurveyID = model.id_pesquisa;
                user.token = SessionKey;
                user.cd_externo = model.cd_chave_externo;

                if (VerificarParticipante(model.cd_chave_externo, model.id_pesquisa) == true)
                {
                    JObject UpdateAccProfile = new JObject(
                    new JProperty("user", user));
                    var _json_user = JObject.Parse(UpdateAccProfile.ToString());

                    client.Method = "add_participants";
                    client.Parameters.Add("sSessionKey", Convert.ToString(SessionKey));
                    client.Parameters.Add("iSurveyID", user.txtiSurveyID);
                    client.Parameters.Add("aParticipantData", _json_user);
                    client.Parameters.Add("bCreateToken", 0);
                    var _ret = client.Post();
                    client.Post();


                    if (string.IsNullOrEmpty(model.id_envio_pesquisa) || string.IsNullOrEmpty(model.emailParticipante))
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Erro, verificar informações do participante" + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.BadRequest
                        };
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }

                    if (!string.IsNullOrEmpty(_ret) || (_ret.Length > 90))
                    {

                        var idres = _ret.Split(new[] { "tid" }, StringSplitOptions.None)[1];
                        idres = idres.Substring(4, 3);
                        model.id_usuario_retorno = idres;



                        tbl_pesquisa_usuario _participante = new tbl_pesquisa_usuario();
                        _participante.cd_chave_externo = model.cd_chave_externo;

                        _participante.id_pesquisa = Convert.ToInt32(model.id_pesquisa);

                        _participante.id_envio_pesquisa = Convert.ToInt32(model.id_envio_pesquisa);

                        _participante.id_usuario_retorno = Convert.ToString(idres);

                        _participante.id_token_retorno = SessionKey;

                        _participante.dt_cadastro = DateTime.Now;

                        SalvarParticipante(model, _participante);
                        var resp = new HttpResponseMessage(HttpStatusCode.Created)
                        {
                            Content = new StringContent("Participante Adicionado" + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.Created
                        };
                        if (exeMtds == true)
                        {
                            ConvidarParticipante(model);
                        }
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }
                    else
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Erro, verificar informações de usuário, objeto nulo.  " + Environment.NewLine + _ret, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.BadRequest
                        };
                        client.ClearParameters();
                        throw new HttpResponseException(resp);
                    }
                }
                else
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                    {
                        Content = new StringContent("Erro, usuário já existente  ", System.Text.Encoding.UTF8, "text/plain"),
                        StatusCode = HttpStatusCode.MethodNotAllowed
                    };
                    client.ClearParameters();
                    throw new HttpResponseException(resp);

                }
            }
            var res = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Erro, token inválido  ", System.Text.Encoding.UTF8, "text/plain"),
                StatusCode = HttpStatusCode.BadRequest
            };
            client.ClearParameters();
            throw new HttpResponseException(res);
        }

        [ResponseType(typeof(tbl_pesquisa_usuario))]
        public bool VerificarParticipante(string cd_chave_externo, string id_pesquisa)
        {

            int x = 0;
            Int32.TryParse(id_pesquisa, out x);

            using (var ctx = new isbet_pesquisa_desEntities())
            {
                tbl_pesquisa_usuario PesquisaUsuarioObj = new tbl_pesquisa_usuario();
                var lst_PesquisaUsuario = ctx.tbl_pesquisa_usuario
                            .Where(c => c.id_pesquisa == x)
                            .Where(c => c.cd_chave_externo == cd_chave_externo);


                if ((lst_PesquisaUsuario != null) && (lst_PesquisaUsuario.Count() > 0))
                {


                    string msg = "Usuário já existe";
                    Console.Write(msg);
                    return false;
                    throw new Exception("Usuário já existe");

                }
                else
                    return true;



            }


        }

        [ResponseType(typeof(tbl_pesquisa_usuario))]
        public void SalvarParticipante(Participante model, tbl_pesquisa_usuario _participante)
        {

            using (var ctx = new isbet_pesquisa_desEntities())
            {
                ctx.tbl_pesquisa_usuario.Add(_participante);
                ctx.SaveChanges();
            }
            return;
        }

    }
}